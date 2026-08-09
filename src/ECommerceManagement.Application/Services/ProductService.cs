using ECommerceManagement.Application.DTOs.Catalog;
using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Domain.Entities;

namespace ECommerceManagement.Application.Services;

public class ProductService : IProductService
{
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IGenericRepository<Product> productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ProductDto>> GetProductsBySellerIdAsync(int sellerId)
    {
        var allProducts = await _productRepository.GetAllAsync();
        
        return allProducts
            .Where(p => p.SellerId == sellerId && p.IsActive)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                SellerId = p.SellerId,
                Name = p.Name,
                Sku = p.Sku,
                Price = p.Price,
                Quantity = p.Quantity
            });
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null || !product.IsActive) return null;

        return new ProductDto
        {
            Id = product.Id,
            SellerId = product.SellerId,
            Name = product.Name,
            Sku = product.Sku,
            Price = product.Price,
            Quantity = product.Quantity
        };
    }

    public async Task AddAsync(CreateProductDto dto)
    {
        // İş Kuralları
        if (dto.Price <= 0)
            throw new InvalidOperationException("Ürün fiyatı 0'dan büyük olmalıdır.");

        if (dto.Quantity < 0)
            throw new InvalidOperationException("Stok miktarı negatif olamaz.");

        var product = new Product
        {
            SellerId = dto.SellerId,
            CategoryId = dto.CategoryId,
            WarehouseId = dto.WarehouseId,
            Name = dto.Name,
            Sku = dto.Sku,
            Price = dto.Price,
            Quantity = dto.Quantity,
            IsActive = true
        };

        await _productRepository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateAsync(UpdateProductDto dto)
    {
        var product = await _productRepository.GetByIdAsync(dto.Id);
        if (product == null || !product.IsActive)
            throw new KeyNotFoundException("Güncellenecek ürün bulunamadı.");

        if (dto.Price <= 0)
            throw new InvalidOperationException("Ürün fiyatı 0'dan büyük olmalıdır.");

        if (dto.Quantity < 0)
            throw new InvalidOperationException("Stok miktarı negatif olamaz.");

        product.Price = dto.Price;
        product.Quantity = dto.Quantity;
        product.IsActive = dto.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        _productRepository.Update(product);
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