using AutoMapper;
using ECommerceManagement.Application.DTOs.Catalog;
using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Domain.Entities;
using ECommerceManagement.Domain.Enums;
using SysmondAx.Integration.Services.Stock;
using SysmondAx.Integration.Models.Requests;

namespace ECommerceManagement.Application.Services;

public class ProductService : IProductService
{
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IGenericRepository<ProductMovement> _productMovementRepository;
    private readonly IGenericRepository<Warehouse> _warehouseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ISysmondStockService _sysmondStockService;

    public ProductService(
        IGenericRepository<Product> productRepository, 
        IGenericRepository<ProductMovement> productMovementRepository, 
        IGenericRepository<Warehouse> warehouseRepository,
        IUnitOfWork unitOfWork, 
        IMapper mapper,
        ISysmondStockService sysmondStockService)
    {
        _productRepository = productRepository;
        _productMovementRepository = productMovementRepository;
        _warehouseRepository = warehouseRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _sysmondStockService = sysmondStockService;
    }

    public async Task<IEnumerable<SellerProductDto>> GetProductsBySellerIdAsync(int sellerId)
    {
        var sellerProducts = await _productRepository.GetWhereAsync(p => p.SellerId == sellerId);
        return _mapper.Map<IEnumerable<SellerProductDto>>(sellerProducts);
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null || !product.IsActive) return null;

        return _mapper.Map<ProductDto>(product);
    }

    public async Task AddAsync(CreateProductDto dto)
    {
        if (dto.Price <= 0)
            throw new InvalidOperationException("Ürün fiyatı 0'dan büyük olmalıdır.");

        if (dto.Quantity < 0)
            throw new InvalidOperationException("Stok miktarı negatif olamaz.");

        var product = _mapper.Map<Product>(dto);
        product.IsActive = true;
        
        var sysmondRequest = new SysmondStockRequest
        {
            Name = dto.Name,
            Code = dto.Sku,
            SalePrice = dto.Price,
            PurchasePrice = dto.Price,
            VatPercent = 0
        };

        var (sysmondStockId, sysmondStockPriceId) = await _sysmondStockService.CreateStockAsync(sysmondRequest);

        if (sysmondStockId != Guid.Empty)
        {
            product.SysmondStockId = sysmondStockId;
            
            var warehouse = await _warehouseRepository.GetByIdAsync(dto.WarehouseId); 
            
            if (warehouse != null && warehouse.SysmondId.HasValue)
            {
                await _sysmondStockService.MapStockToWarehouseAsync(sysmondStockId, warehouse.SysmondId.Value);

                if (dto.Quantity > 0)
                {
                    Guid receiptId = await _sysmondStockService.CreateStockReceiptAsync(
                        warehouse.SysmondId.Value, 
                        $"Sistemden Otomatik Açılış Stoğu - {dto.Name}");

                    await _sysmondStockService.AddStockReceiptItemAsync(
                        receiptId, 
                        sysmondStockId, 
                        warehouse.SysmondId.Value, 
                        dto.Quantity, 
                        dto.Price,
                        sysmondStockPriceId);

                    await _sysmondStockService.ProcessStockReceiptAsync(receiptId);
                }
            }
        }

        await _productRepository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        if (product.Quantity > 0)
        {
            await _productMovementRepository.AddAsync(new ProductMovement
            {
                ProductId = product.Id,
                MovementType = MovementType.Entry,
                Quantity = product.Quantity,
                CreatedAt = DateTime.UtcNow
            });
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task UpdateProductAsync(int id, UpdateProductDto dto)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
            throw new KeyNotFoundException("Ürün bulunamadı.");

        int oldQuantity = product.Quantity;
        int newQuantity = dto.Quantity;

        _mapper.Map(dto, product);
        _productRepository.Update(product);

        if (newQuantity > oldQuantity)
        {
            int diff = newQuantity - oldQuantity;
            await _productMovementRepository.AddAsync(new ProductMovement
            {
                ProductId = product.Id,
                Quantity = diff,
                MovementType = MovementType.Entry,
                CreatedAt = DateTime.UtcNow
            });
        }
        else if (newQuantity < oldQuantity)
        {
            int diff = oldQuantity - newQuantity;
            await _productMovementRepository.AddAsync(new ProductMovement
            {
                ProductId = product.Id,
                Quantity = diff,
                MovementType = MovementType.Exit,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteProductAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
        {
            throw new KeyNotFoundException($"ID'si {id} olan ürün bulunamadı.");
        }

        if (product.SysmondStockId.HasValue)
        {
            await _sysmondStockService.DeleteStockAsync(product.SysmondStockId.Value);
        }

        _productRepository.Delete(product);

        await _unitOfWork.SaveChangesAsync();
    }
}