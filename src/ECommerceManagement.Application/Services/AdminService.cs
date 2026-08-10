using AutoMapper;
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
    private readonly IMapper _mapper;

    public AdminService(
        IGenericRepository<Category> categoryRepository,
        IGenericRepository<Warehouse> warehouseRepository,
        IGenericRepository<UserRole> userRoleRepository,
        IGenericRepository<UserPermission> userPermissionRepository,
        IDistributedCache cache,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _categoryRepository = categoryRepository;
        _warehouseRepository = warehouseRepository;
        _userRoleRepository = userRoleRepository;
        _userPermissionRepository = userPermissionRepository;
        _cache = cache;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }
    
    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<CategoryDto>>(categories);
    }

    public async Task CreateCategoryAsync(CreateCategoryDto dto)
    {
        var category = _mapper.Map<Category>(dto);
        await _categoryRepository.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();
    }
    
    public async Task<IEnumerable<WarehouseDto>> GetAllWarehousesAsync()
    {
        var warehouses = await _warehouseRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<WarehouseDto>>(warehouses);
    }

    public async Task CreateWarehouseAsync(CreateWarehouseDto dto)
    {
        var warehouse = _mapper.Map<Warehouse>(dto);
        warehouse.IsActive = true;
        await _warehouseRepository.AddAsync(warehouse);
        await _unitOfWork.SaveChangesAsync();
    }
    
    public async Task AssignRoleToUserAsync(AssignRoleDto dto)
    {
        var existingRoles = await _userRoleRepository.GetAllAsync();
        var hasRole = existingRoles.Any(ur => ur.UserId == dto.UserId && ur.RoleId == dto.RoleId);

        if (!hasRole)
        {
            var userRole = new UserRole { UserId = dto.UserId, RoleId = dto.RoleId };
            await _userRoleRepository.AddAsync(userRole);
            await _unitOfWork.SaveChangesAsync();

            await _cache.RemoveAsync($"permissions_user_{dto.UserId}");
        }
    }


    public async Task AssignPermissionToUserAsync(AssignUserPermissionDto dto)
    {
        var existingPermissions = await _userPermissionRepository.GetAllAsync();
        var userPerm = existingPermissions.FirstOrDefault(up => up.UserId == dto.UserId && up.PermissionId == dto.PermissionId);

        if (userPerm != null)
        {
            userPerm.IsGranted = dto.IsGranted;
            _userPermissionRepository.Update(userPerm);
        }
        else
        {
            var newPerm = new UserPermission
            {
                UserId = dto.UserId,
                PermissionId = dto.PermissionId,
                IsGranted = dto.IsGranted
            };
            await _userPermissionRepository.AddAsync(newPerm);
        }

        await _unitOfWork.SaveChangesAsync();

        await _cache.RemoveAsync($"permissions_user_{dto.UserId}");
    }
}