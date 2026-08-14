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
        // 1. Sysmond'dan ürünleri ve depo-stok ilişkilerini çek
        var sysmondProducts = await _sysmondStockService.GetProductsAsync();
        var sysmondWarehouseStocks = await _sysmondWarehouseService.GetWarehouseStocksAsync();

        // 2. FİYATLARI PARALEL OLARAK ÇEK (Sysmond'un zorunlu stockId kuralına uygun ve çok hızlı)
        var priceTasks = sysmondProducts.Select(async sp =>
        {
            var priceDto = await _sysmondStockService.GetStockPriceAsync(sp.Id);
            return new { StockId = sp.Id, Price = priceDto?.UnitPrice ?? 0m };
        });

        var priceResults = await Task.WhenAll(priceTasks);
        var priceDictionary = priceResults.ToDictionary(p => p.StockId, p => p.Price);

        // 3. Lokal verilerimizi çek
        var localProducts = await _productRepository.GetAllAsync();
        var localWarehouses = await _warehouseRepository.GetAllAsync();

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

            // Sözlükten ürünün fiyatını alıyoruz
            decimal resolvedPrice = priceDictionary.TryGetValue(sysProduct.Id, out var price) ? price : 0m;

            if (existingProduct == null)
            {
                // Yeni ürün ekleniyor
                var newProduct = new Product
                {
                    Name = sysProduct.Name,
                    Sku = sysProduct.Code ?? $"SKU-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                    Price = resolvedPrice,
                    Quantity = (int)sysProduct.AmountInWarehouse,
                    IsActive = sysProduct.IsActive,
                    SysmondStockId = sysProduct.Id,
                    CategoryId = 4,
                    SellerId = 1,
                    WarehouseId = resolvedWarehouseId 
                };
                await _productRepository.AddAsync(newProduct);
            }
            else
            {
                // Var olan ürün güncelleniyor
                existingProduct.Name = sysProduct.Name;
                existingProduct.Price = resolvedPrice;
                existingProduct.Quantity = (int)sysProduct.AmountInWarehouse;
                existingProduct.IsActive = sysProduct.IsActive;
                existingProduct.WarehouseId = resolvedWarehouseId;
                _productRepository.Update(existingProduct);
            }
        }

        // 4. TEMİZLİK AŞAMASI: Sysmond'da silinenleri lokal DB'den temizle
        foreach (var localProduct in localProducts)
        {
            if (localProduct.SysmondStockId.HasValue && !sysmondProductIds.Contains(localProduct.SysmondStockId.Value))
            {
                _productRepository.Delete(localProduct);
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }
    
    
}