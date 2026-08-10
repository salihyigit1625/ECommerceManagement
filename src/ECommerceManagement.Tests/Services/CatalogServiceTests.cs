using AutoMapper;
using ECommerceManagement.Application.Common;
using ECommerceManagement.Application.DTOs.Catalog;
using ECommerceManagement.Application.Services;
using ECommerceManagement.Domain.Entities;
using ECommerceManagement.Application.Interfaces;
using FluentAssertions;
using Moq;
using System.Linq.Expressions;

namespace ECommerceManagement.Tests.Services;

public class CatalogServiceTests
{
    private readonly Mock<IGenericRepository<Product>> _mockProductRepo;
    private readonly Mock<IMapper> _mockMapper;
    private readonly CatalogService _catalogService;

    public CatalogServiceTests()
    {
        _mockProductRepo = new Mock<IGenericRepository<Product>>();
        _mockMapper = new Mock<IMapper>();

        _catalogService = new CatalogService(
            _mockProductRepo.Object,
            _mockMapper.Object
        );
    }

    [Fact]
    public async Task GetActiveProductsPagedAsync_Should_Return_Paged_Result_With_Mapped_Dtos()
    {
        // Arrange
        var filter = new ProductFilterDto
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "Laptop",
            SortBy = "price",
            SortOrder = "asc"
        };

        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Gaming Laptop", Price = 20000, IsActive = true, Quantity = 5 }
        };

        var productDtos = new List<ProductDto>
        {
            new ProductDto { Id = 1, Name = "Gaming Laptop", Price = 20000 }
        };

        _mockProductRepo.Setup(r => r.GetPagedAsync(
            It.IsAny<Expression<Func<Product, bool>>?>(),
            It.IsAny<Func<IQueryable<Product>, IOrderedQueryable<Product>>?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<Expression<Func<Product, object>>[]>()
        )).ReturnsAsync((products, 1));

        _mockMapper.Setup(m => m.Map<IEnumerable<ProductDto>>(products))
            .Returns(productDtos);

        // Act
        var result = await _catalogService.GetActiveProductsPagedAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Gaming Laptop");
    }
}