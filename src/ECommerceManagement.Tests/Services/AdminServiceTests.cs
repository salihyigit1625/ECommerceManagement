using AutoMapper;
using ECommerceManagement.Application.DTOs.Auth;
using ECommerceManagement.Application.DTOs.Catalog;
using ECommerceManagement.Application.Services;
using ECommerceManagement.Domain.Entities;
using ECommerceManagement.Application.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using SysmondAx.Integration.Services.Warehouse;

namespace ECommerceManagement.Tests.Services;

public class AdminServiceTests
{
    private readonly Mock<IGenericRepository<Category>> _mockCategoryRepo;
    private readonly Mock<IGenericRepository<Warehouse>> _mockWarehouseRepo;
    private readonly Mock<IGenericRepository<UserRole>> _mockUserRoleRepo;
    private readonly Mock<IGenericRepository<UserPermission>> _mockUserPermissionRepo;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly AdminService _adminService;
    private readonly Mock<ISysmondWarehouseService> _mockSysmondWarehouseService;

    public AdminServiceTests()
    {
        _mockCategoryRepo = new Mock<IGenericRepository<Category>>();
        _mockWarehouseRepo = new Mock<IGenericRepository<Warehouse>>();
        _mockUserRoleRepo = new Mock<IGenericRepository<UserRole>>();
        _mockUserPermissionRepo = new Mock<IGenericRepository<UserPermission>>();
        _mockUow = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockCache = new Mock<IDistributedCache>();
        _mockSysmondWarehouseService = new Mock<ISysmondWarehouseService>();

        _adminService = new AdminService(
            _mockCategoryRepo.Object,
            _mockWarehouseRepo.Object,
            _mockUserRoleRepo.Object,
            _mockUserPermissionRepo.Object,
            _mockCache.Object,
            _mockUow.Object,
            _mockMapper.Object,
            _mockSysmondWarehouseService.Object
        );
    }

    [Fact]
    public async Task AssignRoleToUserAsync_Should_Not_Add_Role_Or_Clear_Cache_When_User_Already_Has_Role()
    {
        // Arrange: Kullanıcı zaten RoleId = 2'ye sahip
        int userId = 5;
        int roleId = 2;
        var existingRole = new UserRole { UserId = userId, RoleId = roleId };
        var dto = new AssignRoleDto { UserId = userId, RoleId = roleId };

        _mockUserRoleRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<UserRole> { existingRole });

        // Act
        await _adminService.AssignRoleToUserAsync(dto);

        // Assert
        _mockUserRoleRepo.Verify(r => r.AddAsync(It.IsAny<UserRole>()), Times.Never);
        _mockUow.Verify(u => u.SaveChangesAsync(), Times.Never);
        _mockCache.Verify(c => c.RemoveAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateWarehouseAsync_Should_Set_IsActive_True_And_Save()
    {
        // Arrange
        var dto = new CreateWarehouseDto { Name = "Ana Depo", Location = "İstanbul" };
        var warehouseEntity = new Warehouse { Name = "Ana Depo", Location = "İstanbul" };

        _mockMapper.Setup(m => m.Map<Warehouse>(dto)).Returns(warehouseEntity);

        // Act
        await _adminService.CreateWarehouseAsync(dto);

        // Assert
        warehouseEntity.IsActive.Should().BeTrue();
        _mockWarehouseRepo.Verify(r => r.AddAsync(warehouseEntity), Times.Once);
        _mockUow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}