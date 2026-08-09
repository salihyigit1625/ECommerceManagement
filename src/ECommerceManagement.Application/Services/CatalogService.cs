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
        // İlişkili tablolardan verileri Include ile çekiyoruz
        var products = await _productRepository.GetAllAsync(
            p => p.Category, 
            p => p.Seller, 
            p => p.Warehouse
        );

        return products
            .Where(p => p.IsActive && p.Quantity > 0)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                Price = p.Price,
                Quantity = p.Quantity,
                SellerId = p.SellerId,
                CompanyName = p.Seller != null ? p.Seller.CompanyName : string.Empty,
                CategoryName = p.Category != null ? p.Category.Name : string.Empty,
                WarehouseName = p.Warehouse != null ? p.Warehouse.Name : string.Empty
            });
    }
}