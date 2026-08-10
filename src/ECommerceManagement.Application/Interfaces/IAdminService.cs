using ECommerceManagement.Application.DTOs.Auth;
using ECommerceManagement.Application.DTOs.Catalog;

namespace ECommerceManagement.Application.Interfaces;

public interface IAdminService
{
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
    Task CreateCategoryAsync(CreateCategoryDto dto);

    Task<IEnumerable<WarehouseDto>> GetAllWarehousesAsync();
    Task CreateWarehouseAsync(CreateWarehouseDto dto);
    
    Task AssignRoleToUserAsync(AssignRoleDto dto);
    Task AssignPermissionToUserAsync(AssignUserPermissionDto dto);
}