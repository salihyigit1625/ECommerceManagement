using ECommerceManagement.Application.DTOs.Catalog;
using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Domain.Entities;

namespace ECommerceManagement.Application.Services;

public class CatalogService : ICatalogService
{
    private readonly IGenericRepository<Product> _productRepository;

    public CatalogService(IGenericRepository<Product> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IEnumerable<ProductDto>> GetActiveProductsAsync()
    {
        var products = await _productRepository.GetAllAsync();
        
        // Müşteriye sadece aktif ve stoğu 0'dan büyük olan ürünleri listeliyoruz
        return products
            .Where(p => p.IsActive && p.Quantity > 0)
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
}