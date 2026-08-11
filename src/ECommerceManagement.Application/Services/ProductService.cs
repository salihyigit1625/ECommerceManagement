using AutoMapper;
using ECommerceManagement.Application.DTOs.Catalog;
using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Domain.Entities;
using ECommerceManagement.Domain.Enums;

namespace ECommerceManagement.Application.Services;

public class ProductService : IProductService
{
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IGenericRepository<ProductMovement> _productMovementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductService(
        IGenericRepository<Product> productRepository, 
        IGenericRepository<ProductMovement> productMovementRepository, 
        IUnitOfWork unitOfWork, 
        IMapper mapper)
    {
        _productRepository = productRepository;
        _productMovementRepository = productMovementRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<SellerProductDto>> GetProductsBySellerIdAsync(int sellerId)
    {
        // PERFORMANS: Tüm tabloyu RAM'e almak yerine doğrudan veritabanı seviyesinde filtreleme yapıldı.
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

    public async Task UpdateAsync(UpdateProductDto dto)
    {
        var product = await _productRepository.GetByIdAsync(dto.Id);
        if (product == null)
            throw new KeyNotFoundException("Güncellenecek ürün bulunamadı.");

        if (dto.Price <= 0)
            throw new InvalidOperationException("Ürün fiyatı 0'dan büyük olmalıdır.");

        if (dto.Quantity < 0)
            throw new InvalidOperationException("Stok miktarı negatif olamaz.");

        int stockDifference = dto.Quantity - product.Quantity;

        _mapper.Map(dto, product);
        product.UpdatedAt = DateTime.UtcNow;

        _productRepository.Update(product);

        if (stockDifference != 0)
        {
            await _productMovementRepository.AddAsync(new ProductMovement
            {
                ProductId = product.Id,
                MovementType = stockDifference > 0 ? MovementType.Entry : MovementType.Exit,
                Quantity = Math.Abs(stockDifference),
                CreatedAt = DateTime.UtcNow
            });
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
            throw new KeyNotFoundException("Silinecek ürün bulunamadı.");

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync();
    }
}