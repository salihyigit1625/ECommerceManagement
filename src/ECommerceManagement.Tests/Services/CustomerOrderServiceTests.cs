using ECommerceManagement.Application.DTOs.Orders;
using ECommerceManagement.Application.Services;
using ECommerceManagement.Domain.Entities;
using ECommerceManagement.Domain.Enums;
using ECommerceManagement.Application.Interfaces;
using FluentAssertions;
using Moq;
using System.Linq.Expressions;

namespace ECommerceManagement.Tests.Services;

public class CustomerOrderServiceTests
{
    private readonly Mock<IGenericRepository<Order>> _mockOrderRepo;
    private readonly Mock<IGenericRepository<Product>> _mockProductRepo;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly CustomerOrderService _orderService;

    public CustomerOrderServiceTests()
    {
        _mockOrderRepo = new Mock<IGenericRepository<Order>>();
        _mockProductRepo = new Mock<IGenericRepository<Product>>();
        _mockUow = new Mock<IUnitOfWork>();

        _orderService = new CustomerOrderService(
            _mockOrderRepo.Object,
            _mockProductRepo.Object,
            _mockUow.Object
        );
    }

    [Fact]
    public async Task CreateOrderAsync_Should_Throw_ArgumentException_When_Cart_Is_Empty()
    {
        var dto = new CreateOrderDto { CustomerId = 1, Items = new List<CreateOrderItemDto>() };
        Func<Task> act = async () => await _orderService.CreateOrderAsync(dto);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Sepet boş olamaz.");
    }

    [Fact]
    public async Task CreateOrderAsync_Should_Throw_Exception_When_Product_Is_Inactive()
    {
        // Arrange: Ürün pasif (IsActive = false)
        var product = new Product { Id = 1, Name = "Laptop", Quantity = 10, IsActive = false, Price = 1000 };
        _mockProductRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Product> { product });

        var dto = new CreateOrderDto
        {
            CustomerId = 1,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { ProductId = 1, Quantity = 1 } }
        };

        // Act
        Func<Task> act = async () => await _orderService.CreateOrderAsync(dto);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("*satıştan kaldırılmış*");
    }

    [Fact]
    public async Task CancelMyOrderAsync_Should_Throw_KeyNotFoundException_When_Order_Belongs_To_Another_Customer()
    {
        // Arrange: Sipariş başkasına ait (CustomerId = 99)
        var order = new Order { Id = 10, CustomerId = 99, Status = OrderStatus.Pending };
        _mockOrderRepo.Setup(r => r.GetByIdAsync(10, It.IsAny<Expression<Func<Order, object?>>>())).ReturnsAsync(order);

        // Act: CustomerId = 1 siparişi iptal etmeye çalışıyor
        Func<Task> act = async () => await _orderService.CancelMyOrderAsync(10, 1);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Sipariş bulunamadı*");
    }

    [Fact]
    public async Task CancelMyOrderAsync_Should_Throw_InvalidOperationException_When_Order_Is_Already_Shipped()
    {
        // Arrange: Sipariş kargolanmış
        var order = new Order { Id = 10, CustomerId = 1, Status = OrderStatus.Shipped };
        _mockOrderRepo.Setup(r => r.GetByIdAsync(10, It.IsAny<Expression<Func<Order, object?>>>())).ReturnsAsync(order);

        // Act
        Func<Task> act = async () => await _orderService.CancelMyOrderAsync(10, 1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Bu sipariş iptal edilemez (Faturası kesilmiş veya kargolanmış olabilir).");
    }
}