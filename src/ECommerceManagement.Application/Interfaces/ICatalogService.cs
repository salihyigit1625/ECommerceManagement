using ECommerceManagement.Application.Common;
using ECommerceManagement.Application.DTOs.Catalog;

namespace ECommerceManagement.Application.Interfaces;

public interface ICatalogService
{
    Task<PagedResultDto<ProductDto>> GetActiveProductsPagedAsync(ProductFilterDto filter);
    Task SyncProductsAsync();
}