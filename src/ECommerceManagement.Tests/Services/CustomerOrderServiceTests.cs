using ECommerceManagement.Application.DTOs.Orders;
using ECommerceManagement.Application.Services;
using ECommerceManagement.Domain.Entities;
using ECommerceManagement.Domain.Enums;
using ECommerceManagement.Application.Interfaces;
using FluentAssertions;
using Moq;
using System.Linq.Expressions;
using SysmondAx.Integration.Services.Order;
using SysmondAx.Integration.Services.Stock;
using SysmondAx.Integration.Models.Dtos;
using Xunit;

namespace ECommerceManagement.Tests.Services;

public class CustomerOrderServiceTests
{
    private readonly Mock<IGenericRepository<Order>> _mockOrderRepo;
    private readonly Mock<IGenericRepository<Product>> _mockProductRepo;
    private readonly Mock<IGenericRepository<Address>> _mockAddressRepo;
    private readonly Mock<IGenericRepository<ProductMovement>> _mockProductMovementRepo;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<ISysmondOrderService> _mockSysmondOrderService;
    private readonly Mock<ISysmondStockService> _mockSysmondStockService;
    private readonly CustomerOrderService _orderService;

    public CustomerOrderServiceTests()
    {
        _mockOrderRepo = new Mock<IGenericRepository<Order>>();
        _mockProductRepo = new Mock<IGenericRepository<Product>>();
        _mockAddressRepo = new Mock<IGenericRepository<Address>>();
        _mockProductMovementRepo = new Mock<IGenericRepository<ProductMovement>>();
        _mockUow = new Mock<IUnitOfWork>();
        _mockSysmondOrderService = new Mock<ISysmondOrderService>();
        _mockSysmondStockService = new Mock<ISysmondStockService>();

        _orderService = new CustomerOrderService(
            _mockOrderRepo.Object,
            _mockProductRepo.Object,
            _mockAddressRepo.Object,
            _mockProductMovementRepo.Object,
            _mockUow.Object,
            _mockSysmondOrderService.Object,
            _mockSysmondStockService.Object
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

        _mockOrderRepo.Setup(r => r.AddAsync(It.IsAny<Order>())).Callback<Order>(o => o.Id = 99); 

        _mockSysmondOrderService.Setup(s => s.CreateDraftOrderAsync(It.IsAny<SysmondOrderDraftCreateDto>()))
                                .ReturnsAsync(Guid.NewGuid());

        await _orderService.CreateOrderAsync(dto);

        _mockOrderRepo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
        _mockProductRepo.Verify(r => r.Update(It.Is<Product>(p => p.Quantity == 8)), Times.Once);
        _mockProductMovementRepo.Verify(m => m.AddAsync(It.Is<ProductMovement>(pm => 
            pm.ProductId == 1 && pm.MovementType == MovementType.Exit && pm.Quantity == 2 && pm.ReferenceId == 99)), Times.Once);

        _mockUow.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }
}