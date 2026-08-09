using ECommerceManagement.Application.DTOs.Catalog;

namespace ECommerceManagement.Application.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetProductsBySellerIdAsync(int sellerId);
    Task<ProductDto?> GetByIdAsync(int id);
    Task AddAsync(CreateProductDto dto);
    Task UpdateAsync(UpdateProductDto dto);
    Task DeleteAsync(int id); // Soft-delete (IsActive = false)
}