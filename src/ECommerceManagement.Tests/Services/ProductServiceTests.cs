using AutoMapper;
using ECommerceManagement.Application.DTOs.Catalog;
using ECommerceManagement.Application.Services;
using ECommerceManagement.Domain.Entities;
using ECommerceManagement.Application.Interfaces;
using FluentAssertions;
using Moq;

namespace ECommerceManagement.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IGenericRepository<Product>> _mockProductRepo;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IMapper> _mockMapper;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _mockProductRepo = new Mock<IGenericRepository<Product>>();
        _mockUow = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _productService = new ProductService(
            _mockProductRepo.Object,
            _mockUow.Object,
            _mockMapper.Object
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task AddAsync_Should_Throw_InvalidOperationException_When_Price_Is_Invalid(decimal invalidPrice)
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "Test Ürün",
            Price = invalidPrice,
            Quantity = 10
        };

        // Act
        Func<Task> act = async () => await _productService.AddAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Ürün fiyatı 0'dan büyük olmalıdır.");
    }

    [Fact]
    public async Task AddAsync_Should_Throw_InvalidOperationException_When_Quantity_Is_Negative()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "Test Ürün",
            Price = 100,
            Quantity = -5
        };

        // Act
        Func<Task> act = async () => await _productService.AddAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Stok miktarı negatif olamaz.");
    }

    [Fact]
    public async Task AddAsync_Should_Add_Product_And_Set_IsActive_True_When_Valid()
    {
        // Arrange
        var dto = new CreateProductDto { Name = "Laptop", Price = 15000, Quantity = 20 };
        var productEntity = new Product { Name = "Laptop", Price = 15000, Quantity = 20 };

        _mockMapper.Setup(m => m.Map<Product>(dto)).Returns(productEntity);

        // Act
        await _productService.AddAsync(dto);

        // Assert
        productEntity.IsActive.Should().BeTrue();
        _mockProductRepo.Verify(r => r.AddAsync(productEntity), Times.Once);
        _mockUow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Should_Perform_Soft_Delete_By_Setting_IsActive_False()
    {
        // Arrange
        var existingProduct = new Product { Id = 1, Name = "Klavye", IsActive = true };

        _mockProductRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(existingProduct);

        // Act
        await _productService.DeleteAsync(1);

        // Assert
        // Fiziksel olarak DB'den silinmedi, IsActive = false yapıldı (Soft Delete)
        existingProduct.IsActive.Should().BeFalse();
        
        _mockProductRepo.Verify(r => r.Update(existingProduct), Times.Once);
        _mockUow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}