using ECommerceManagement.Application.DTOs.Catalog;

namespace ECommerceManagement.Application.Interfaces;

public interface ICatalogService
{
    Task<IEnumerable<ProductDto>> GetActiveProductsAsync();
}