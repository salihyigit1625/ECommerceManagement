using AutoMapper;
using ECommerceManagement.Application.DTOs.Catalog;
using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Application.Services;
using ECommerceManagement.Domain.Entities;
using ECommerceManagement.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace ECommerceManagement.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IGenericRepository<Product>> _mockProductRepo;
    private readonly Mock<IGenericRepository<ProductMovement>> _mockMovementRepo;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly IMapper _mapper;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _mockProductRepo = new Mock<IGenericRepository<Product>>();
        _mockMovementRepo = new Mock<IGenericRepository<ProductMovement>>();
        _mockUow = new Mock<IUnitOfWork>();

        var services = new ServiceCollection();
        
        services.AddLogging(); 
        
        services.AddAutoMapper(cfg => 
        {
            cfg.CreateMap<Product, ProductDto>();
            cfg.CreateMap<Product, SellerProductDto>();
            cfg.CreateMap<CreateProductDto, Product>();
            cfg.CreateMap<UpdateProductDto, Product>();
        });
        
        var serviceProvider = services.BuildServiceProvider();
        _mapper = serviceProvider.GetRequiredService<IMapper>();

        _productService = new ProductService(
            _mockProductRepo.Object,
            _mockMovementRepo.Object,
            _mockUow.Object,
            _mapper
        );
    }

    [Fact]
    public async Task GetProductsBySellerIdAsync_Should_Return_Filtered_Products()
    {
        var products = new List<Product>
        {
            new Product { Id = 1, SellerId = 5, Name = "Laptop" }
        };
        
        _mockProductRepo.Setup(r => r.GetWhereAsync(It.IsAny<Expression<Func<Product, bool>>>()))
                        .ReturnsAsync(products);

        var result = await _productService.GetProductsBySellerIdAsync(5);

        result.Should().NotBeNullOrEmpty();
        result.First().Id.Should().Be(1);
    }

    [Fact]
    public async Task AddAsync_Should_Add_Product_And_Create_Entry_Movement_If_Quantity_Greater_Than_Zero()
    {
        var dto = new CreateProductDto { Name = "Mouse", Price = 500, Quantity = 10, SellerId = 1, CategoryId = 1 };

        await _productService.AddAsync(dto);

        _mockProductRepo.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);
        
        _mockMovementRepo.Verify(m => m.AddAsync(It.Is<ProductMovement>(x => 
            x.MovementType == MovementType.Entry && x.Quantity == 10)), Times.Once);
            
        _mockUow.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task UpdateAsync_Should_Create_Exit_Movement_When_Stock_Decreases()
    {
        var existingProduct = new Product { Id = 1, Name = "Mouse", Price = 500, Quantity = 20 };
        var dto = new UpdateProductDto { Id = 1, Price = 500, Quantity = 15, IsActive = true }; 

        _mockProductRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingProduct);

        await _productService.UpdateAsync(dto);

        _mockProductRepo.Verify(r => r.Update(It.Is<Product>(p => p.Quantity == 15)), Times.Once);
        
        _mockMovementRepo.Verify(m => m.AddAsync(It.Is<ProductMovement>(x => 
            x.MovementType == MovementType.Exit && x.Quantity == 5)), Times.Once);
            
        _mockUow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Should_Not_Create_Movement_When_Stock_Is_Unchanged()
    {
        var existingProduct = new Product { Id = 1, Name = "Mouse", Price = 500, Quantity = 20 };
        var dto = new UpdateProductDto { Id = 1, Price = 700, Quantity = 20, IsActive = true }; 

        _mockProductRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingProduct);

        await _productService.UpdateAsync(dto);

        _mockProductRepo.Verify(r => r.Update(It.IsAny<Product>()), Times.Once);
        
        _mockMovementRepo.Verify(m => m.AddAsync(It.IsAny<ProductMovement>()), Times.Never);
    }
}