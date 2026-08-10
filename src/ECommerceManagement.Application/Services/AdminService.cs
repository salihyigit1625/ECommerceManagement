using ECommerceManagement.Application.DTOs.Auth;
using ECommerceManagement.Application.DTOs.Catalog;
using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace ECommerceManagement.Application.Services;

public class AdminService : IAdminService
{
    private readonly IGenericRepository<Category> _categoryRepository;
    private readonly IGenericRepository<Warehouse> _warehouseRepository;
    private readonly IGenericRepository<UserRole> _userRoleRepository;
    private readonly IGenericRepository<UserPermission> _userPermissionRepository;
    private readonly IDistributedCache _cache;
    private readonly IUnitOfWork _unitOfWork;

    public AdminService(
        IGenericRepository<Category> categoryRepository,
        IGenericRepository<Warehouse> warehouseRepository,
        IGenericRepository<UserRole> userRoleRepository,
        IGenericRepository<UserPermission> userPermissionRepository,
        IDistributedCache cache,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _warehouseRepository = warehouseRepository;
        _userRoleRepository = userRoleRepository;
        _userPermissionRepository = userPermissionRepository;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    // ==========================================
    // KATEGORİ YÖNETİMİ
    // ==========================================
    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        return categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            ParentCategoryId = c.ParentCategoryId
        });
    }

    public async Task CreateCategoryAsync(CreateCategoryDto dto)
    {
        var category = new Category
        {
            Name = dto.Name,
            ParentCategoryId = dto.ParentCategoryId
        };
        await _categoryRepository.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();
    }

    // ==========================================
    // DEPO (WAREHOUSE) YÖNETİMİ
    // ==========================================
    public async Task<IEnumerable<WarehouseDto>> GetAllWarehousesAsync()
    {
        var warehouses = await _warehouseRepository.GetAllAsync();
        return warehouses.Select(w => new WarehouseDto
        {
            Id = w.Id,
            Name = w.Name,
            Location = w.Location
        });
    }

    public async Task CreateWarehouseAsync(CreateWarehouseDto dto)
    {
        var warehouse = new Warehouse
        {
            Name = dto.Name,
            Location = dto.Location,
            IsActive = true
        };
        await _warehouseRepository.AddAsync(warehouse);
        await _unitOfWork.SaveChangesAsync();
    }

    // ==========================================
    // SÜPER ADMIN - ROL ATAMA YÖNETİMİ
    // ==========================================
    public async Task AssignRoleToUserAsync(AssignRoleDto dto)
    {
        var existingRoles = await _userRoleRepository.GetAllAsync();
        var hasRole = existingRoles.Any(ur => ur.UserId == dto.UserId && ur.RoleId == dto.RoleId);

        if (!hasRole)
        {
            var userRole = new UserRole { UserId = dto.UserId, RoleId = dto.RoleId };
            await _userRoleRepository.AddAsync(userRole);
            await _unitOfWork.SaveChangesAsync();

            // Rol değiştiği için kullanıcının Redis önbelleğini (Cache) temizle
            await _cache.RemoveAsync($"permissions_user_{dto.UserId}");
        }
    }

    // ==========================================
    // SÜPER ADMIN - ÖZEL YETKİ EZME (OVERRIDE)
    // ==========================================
    public async Task AssignPermissionToUserAsync(AssignUserPermissionDto dto)
    {
        var existingPermissions = await _userPermissionRepository.GetAllAsync();
        var userPerm = existingPermissions.FirstOrDefault(up => up.UserId == dto.UserId && up.PermissionId == dto.PermissionId);

        if (userPerm != null)
        {
            // Varsa güncelle
            userPerm.IsGranted = dto.IsGranted;
            _userPermissionRepository.Update(userPerm);
        }
        else
        {
            // Yoksa yeni oluştur
            var newPerm = new UserPermission
            {
                UserId = dto.UserId,
                PermissionId = dto.PermissionId,
                IsGranted = dto.IsGranted
            };
            await _userPermissionRepository.AddAsync(newPerm);
        }

        await _unitOfWork.SaveChangesAsync();

        // Kullanıcının özel yetkisi değiştiği için Redis önbelleğini (Cache) temizle
        await _cache.RemoveAsync($"permissions_user_{dto.UserId}");
    }
}