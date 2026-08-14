using ECommerceManagement.Application.DTOs.Catalog;

namespace ECommerceManagement.Application.Interfaces;

public interface IProductService
{
    Task<IEnumerable<SellerProductDto>> GetProductsBySellerIdAsync(int sellerId);
    Task<ProductDto?> GetByIdAsync(int id);
    Task AddAsync(CreateProductDto dto);
    Task UpdateProductAsync(int id, UpdateProductDto dto);
    Task DeleteProductAsync(int id);
}