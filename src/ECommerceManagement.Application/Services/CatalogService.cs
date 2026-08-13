using AutoMapper;
using ECommerceManagement.Application.Common;
using ECommerceManagement.Application.DTOs.Catalog;
using ECommerceManagement.Application.Services;
using ECommerceManagement.Domain.Entities;
using ECommerceManagement.Application.Interfaces;
using SysmondAx.Integration.Services.Stock;
using SysmondAx.Integration.Services.Warehouse;

namespace ECommerceManagement.Application.Services;

public class CatalogService : ICatalogService
{
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IGenericRepository<Warehouse> _warehouseRepository; // <-- Eklendi
    private readonly IMapper _mapper;
    private readonly ISysmondStockService _sysmondStockService;
    private readonly ISysmondWarehouseService _sysmondWarehouseService;
    private readonly IUnitOfWork _unitOfWork;

    public CatalogService(
        IGenericRepository<Product> productRepository, 
        IGenericRepository<Warehouse> warehouseRepository, // <-- Eklendi
        IMapper mapper,
        ISysmondStockService sysmondStockService,
        ISysmondWarehouseService sysmondWarehouseService,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _warehouseRepository = warehouseRepository;
        _mapper = mapper;
        _sysmondStockService = sysmondStockService;
        _sysmondWarehouseService = sysmondWarehouseService;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResultDto<ProductDto>> GetActiveProductsPagedAsync(ProductFilterDto filter)
    {
        System.Linq.Expressions.Expression<Func<Product, bool>> predicate = p =>
            p.IsActive &&
            (string.IsNullOrEmpty(filter.SearchTerm) || p.Name.Contains(filter.SearchTerm) || p.Sku.Contains(filter.SearchTerm)) &&
            (!filter.CategoryId.HasValue || p.CategoryId == filter.CategoryId.Value) &&
            (!filter.MinPrice.HasValue || p.Price >= filter.MinPrice.Value) &&
            (!filter.MaxPrice.HasValue || p.Price <= filter.MaxPrice.Value);

        Func<IQueryable<Product>, IOrderedQueryable<Product>>? orderBy = query =>
        {
            var isDescending = filter.SortOrder.ToLower() == "desc";

            return filter.SortBy?.ToLower() switch
            {
                "price" => isDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                "name" => isDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "quantity" => isDescending ? query.OrderByDescending(p => p.Quantity) : query.OrderBy(p => p.Quantity),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };
        };

        var (items, totalCount) = await _productRepository.GetPagedAsync(
            predicate,
            orderBy,
            filter.PageNumber,
            filter.PageSize,
            p => p.Category!,
            p => p.Seller!,
            p => p.Warehouse!
        );

        var dtos = _mapper.Map<IEnumerable<ProductDto>>(items);

        return new PagedResultDto<ProductDto>(dtos, totalCount, filter.PageNumber, filter.PageSize);
    }
    
    public async Task SyncProductsAsync()
    {

        // 1. Sysmond'dan güncel ürünleri ve depo-stok ilişkilerini sayfalama ile çek
        var sysmondProducts = await _sysmondStockService.GetProductsAsync();
        var sysmondWarehouseStocks = await _sysmondWarehouseService.GetWarehouseStocksAsync();

        // 2. Lokal verilerimizi çek (Ürünler ve Depolar)
        var localProducts = await _productRepository.GetAllAsync();
        var localWarehouses = await _warehouseRepository.GetAllAsync();

        // Sysmond'dan gelen ürün ID'lerinin bir listesini çıkarıyoruz (Hızlı arama için HashSet)
        var sysmondProductIds = sysmondProducts.Select(sp => sp.Id).ToHashSet();

        foreach (var sysProduct in sysmondProducts)
        {
            var existingProduct = localProducts.FirstOrDefault(p => p.SysmondStockId == sysProduct.Id);

            // Depo eşleştirmesi
            var warehouseStockMatch = sysmondWarehouseStocks.FirstOrDefault(ws => ws.StockId == sysProduct.Id);
            int resolvedWarehouseId = localWarehouses.FirstOrDefault()?.Id ?? 1;
            if (warehouseStockMatch != null)
            {
                var matchedLocalWarehouse = localWarehouses.FirstOrDefault(w => w.SysmondId == warehouseStockMatch.WarehouseId);
                if (matchedLocalWarehouse != null)
                {
                    resolvedWarehouseId = matchedLocalWarehouse.Id;
                }
            }

            if (existingProduct == null)
            {
                // Yeni ürün ekleniyor
                var newProduct = new Product
                {
                    Name = sysProduct.Name,
                    Sku = sysProduct.Code ?? $"SKU-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                    Price = 0, 
                    Quantity = (int)sysProduct.AmountInWarehouse,
                    IsActive = sysProduct.IsActive,
                    SysmondStockId = sysProduct.Id,
                    CategoryId = 1,
                    SellerId = 1,
                    WarehouseId = resolvedWarehouseId 
                };
                await _productRepository.AddAsync(newProduct);
            }
            else
            {
                // Var olan ürün güncelleniyor
                existingProduct.Name = sysProduct.Name;
                existingProduct.Quantity = (int)sysProduct.AmountInWarehouse;
                existingProduct.IsActive = sysProduct.IsActive;
                existingProduct.WarehouseId = resolvedWarehouseId;
                _productRepository.Update(existingProduct);
            }
        }

        // 3. TEMİZLİK AŞAMASI: Lokalimizde olup Sysmond'da ARTIK OLMAYAN ürünler
        foreach (var localProduct in localProducts)
        {
            // Eğer ürünün SysmondStockId'si varsa ve güncel Sysmond listesinde yoksa
            if (localProduct.SysmondStockId.HasValue && !sysmondProductIds.Contains(localProduct.SysmondStockId.Value))
            {
                // Veritabanından tamamen silmek için:
                _productRepository.Delete(localProduct); // (Repository metodunun adı Remove ise _productRepository.Remove(localProduct) yapabilirsin)
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }
}