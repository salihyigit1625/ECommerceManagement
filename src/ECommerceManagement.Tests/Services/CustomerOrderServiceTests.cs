using ECommerceManagement.Application.DTOs.Orders;
using ECommerceManagement.Application.Services;
using ECommerceManagement.Domain.Entities;
using ECommerceManagement.Domain.Enums;
using ECommerceManagement.Application.Interfaces;
using FluentAssertions;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace ECommerceManagement.Tests.Services;

public class CustomerOrderServiceTests
{
    private readonly Mock<IGenericRepository<Order>> _mockOrderRepo;
    private readonly Mock<IGenericRepository<Product>> _mockProductRepo;
    private readonly Mock<IGenericRepository<Address>> _mockAddressRepo;
    private readonly Mock<IGenericRepository<ProductMovement>> _mockProductMovementRepo;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly CustomerOrderService _orderService;

    public CustomerOrderServiceTests()
    {
        _mockOrderRepo = new Mock<IGenericRepository<Order>>();
        _mockProductRepo = new Mock<IGenericRepository<Product>>();
        _mockAddressRepo = new Mock<IGenericRepository<Address>>();
        _mockProductMovementRepo = new Mock<IGenericRepository<ProductMovement>>();
        _mockUow = new Mock<IUnitOfWork>();

        _orderService = new CustomerOrderService(
            _mockOrderRepo.Object,
            _mockProductRepo.Object,
            _mockAddressRepo.Object,
            _mockProductMovementRepo.Object,
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
    public async Task CreateOrderAsync_Should_Throw_InvalidOperationException_When_Address_Is_Invalid()
    {
        _mockAddressRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Address?)null);

        var dto = new CreateOrderDto
        {
            CustomerId = 1,
            ShippingAddressId = 10,
            BillingAddressId = 11,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { ProductId = 1, Quantity = 1 } }
        };

        Func<Task> act = async () => await _orderService.CreateOrderAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*teslimat için uygun olmayan*");
    }

    [Fact]
    public async Task CreateOrderAsync_Should_Throw_Exception_When_Product_Is_Inactive()
    {
        var shippingAddr = new Address { Id = 10, CustomerId = 1, IsShipping = true };
        var billingAddr = new Address { Id = 11, CustomerId = 1, IsBilling = true };
        _mockAddressRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(shippingAddr);
        _mockAddressRepo.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(billingAddr);

        var product = new Product { Id = 1, Name = "Laptop", Quantity = 10, IsActive = false, Price = 1000 };
        _mockProductRepo.Setup(r => r.GetWhereAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<Expression<Func<Product, object>>[]>()))
                        .ReturnsAsync(new List<Product> { product });

        var dto = new CreateOrderDto
        {
            CustomerId = 1,
            ShippingAddressId = 10,
            BillingAddressId = 11,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { ProductId = 1, Quantity = 1 } }
        };

        Func<Task> act = async () => await _orderService.CreateOrderAsync(dto);
        await act.Should().ThrowAsync<Exception>().WithMessage("*satıştan kaldırılmış*");
    }

    [Fact]
    public async Task CancelMyOrderAsync_Should_Throw_KeyNotFoundException_When_Order_Belongs_To_Another_Customer()
    {
        var order = new Order { Id = 10, CustomerId = 99, Status = OrderStatus.Pending };
        _mockOrderRepo.Setup(r => r.GetByIdAsync(10, It.IsAny<Expression<Func<Order, object?>>[]>())).ReturnsAsync(order);

        Func<Task> act = async () => await _orderService.CancelMyOrderAsync(10, 1);
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Sipariş bulunamadı*");
    }

    [Fact]
    public async Task CancelMyOrderAsync_Should_Throw_InvalidOperationException_When_Order_Is_Already_Shipped()
    {
        var order = new Order { Id = 10, CustomerId = 1, Status = OrderStatus.Shipped };
        _mockOrderRepo.Setup(r => r.GetByIdAsync(10, It.IsAny<Expression<Func<Order, object?>>[]>())).ReturnsAsync(order);

        Func<Task> act = async () => await _orderService.CancelMyOrderAsync(10, 1);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Bu sipariş iptal edilemez (Faturası kesilmiş veya kargolanmış olabilir).");
    }

    [Fact]
    public async Task CreateOrderAsync_Should_Create_Order_And_Log_Exit_Movement_With_ReferenceId()
    {
        var shippingAddr = new Address { Id = 10, CustomerId = 1, IsShipping = true };
        var billingAddr = new Address { Id = 11, CustomerId = 1, IsBilling = true };
        _mockAddressRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(shippingAddr);
        _mockAddressRepo.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(billingAddr);

        var product = new Product { Id = 1, SellerId = 5, Name = "Laptop", Quantity = 10, IsActive = true, Price = 1000 };
        _mockProductRepo.Setup(r => r.GetWhereAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<Expression<Func<Product, object>>[]>()))
                        .ReturnsAsync(new List<Product> { product });

        var dto = new CreateOrderDto
        {
            CustomerId = 1,
            ShippingAddressId = 10,
            BillingAddressId = 11,
            Items = new List<CreateOrderItemDto> { new CreateOrderItemDto { ProductId = 1, Quantity = 2 } }
        };

        _mockOrderRepo.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(o => o.Id = 99); 

        await _orderService.CreateOrderAsync(dto);

        _mockOrderRepo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
        _mockProductRepo.Verify(r => r.Update(It.Is<Product>(p => p.Quantity == 8)), Times.Once);
        _mockProductMovementRepo.Verify(m => m.AddAsync(It.Is<ProductMovement>(pm => 
            pm.ProductId == 1 && 
            pm.MovementType == MovementType.Exit && 
            pm.Quantity == 2 && 
            pm.ReferenceId == 99)), Times.Once);

        _mockUow.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }
}