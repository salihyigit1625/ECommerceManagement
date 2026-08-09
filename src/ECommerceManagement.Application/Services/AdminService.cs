using ECommerceManagement.Application.DTOs.Catalog;
using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Domain.Entities;

namespace ECommerceManagement.Application.Services;

public class AdminService : IAdminService
{
    private readonly IGenericRepository<Category> _categoryRepository;
    private readonly IGenericRepository<Warehouse> _warehouseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdminService(
        IGenericRepository<Category> categoryRepository,
        IGenericRepository<Warehouse> warehouseRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _warehouseRepository = warehouseRepository;
        _unitOfWork = unitOfWork;
    }

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
}