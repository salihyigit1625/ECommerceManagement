using ECommerceManagement.Application.Constants;
using ECommerceManagement.Application.DTOs.Auth;
using ECommerceManagement.Application.DTOs.Catalog;
using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceManagement.Api.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _adminService.GetAllCategoriesAsync();
            return Ok(categories);
        }

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
        {
            await _adminService.CreateCategoryAsync(dto);
            return Ok(new { Message = "Kategori başarıyla oluşturuldu." });
        }

        [HttpGet("warehouses")]
        public async Task<IActionResult> GetAllWarehouses()
        {
            var warehouses = await _adminService.GetAllWarehousesAsync();
            return Ok(warehouses);
        }

        [HttpPost("warehouses")]
        public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseDto dto)
        {
            await _adminService.CreateWarehouseAsync(dto);
            return Ok(new { Message = "Depo başarıyla oluşturuldu." });
        }
        
        [HasPermission("Users.ManageRoles")] // Sadece Süper Adminlerde olacak yetki
        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto dto)
        {
            await _adminService.AssignRoleToUserAsync(dto);
            return Ok(new { Message = "Rol kullanıcıya başarıyla atandı ve yetki önbelleği temizlendi." });
        }

        [HasPermission("Users.ManagePermissions")] // Sadece Süper Adminlerde olacak yetki
        [HttpPost("assign-permission")]
        public async Task<IActionResult> AssignPermission([FromBody] AssignUserPermissionDto dto)
        {
            await _adminService.AssignPermissionToUserAsync(dto);
            return Ok(new { Message = "Özel yetki ayarı başarıyla kaydedildi ve önbellek güncellendi." });
        }
    }
}