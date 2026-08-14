using AutoMapper;
using ECommerceManagement.Application.DTOs.Invoices;
using ECommerceManagement.Application.DTOs.Orders;
using ECommerceManagement.Application.Services;
using ECommerceManagement.Domain.Entities;
using ECommerceManagement.Domain.Enums;
using ECommerceManagement.Application.Interfaces;
using FluentAssertions;
using Moq;
using System.Linq.Expressions;
using SysmondAx.Integration.Models.Dtos;
using SysmondAx.Integration.Services.Invoice;
using SysmondAx.Integration.Services.Order;
using SysmondAx.Integration.Services.Stock;
using Xunit;

namespace ECommerceManagement.Tests.Services;

public class SellerOrderServiceTests
{
    private readonly Mock<IGenericRepository<Order>> _mockOrderRepo;
    private readonly Mock<IGenericRepository<Invoice>> _mockInvoiceRepo;
    private readonly Mock<IGenericRepository<Customer>> _mockCustomerRepo;
    private readonly Mock<IGenericRepository<Product>> _mockProductRepo;
    private readonly Mock<IGenericRepository<ProductMovement>> _mockMovementRepo;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ISysmondOrderService> _mockSysmondOrderService;
    private readonly Mock<ISysmondStockService> _mockStockService;
    private readonly Mock<ISysmondInvoiceService> _mockSysmondInvoiceService;
    private readonly SellerOrderService _sellerOrderService;

    public SellerOrderServiceTests()
    {
        _mockOrderRepo = new Mock<IGenericRepository<Order>>();
        _mockInvoiceRepo = new Mock<IGenericRepository<Invoice>>();
        _mockCustomerRepo = new Mock<IGenericRepository<Customer>>();
        _mockProductRepo = new Mock<IGenericRepository<Product>>();
        _mockMovementRepo = new Mock<IGenericRepository<ProductMovement>>();
        _mockUow = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockSysmondOrderService = new Mock<ISysmondOrderService>();
        _mockSysmondInvoiceService = new Mock<ISysmondInvoiceService>();
        _mockStockService = new Mock<ISysmondStockService>();

        _sellerOrderService = new SellerOrderService(
            _mockOrderRepo.Object,
            _mockInvoiceRepo.Object,
            _mockCustomerRepo.Object,
            _mockProductRepo.Object,
            _mockMovementRepo.Object,
            _mockSysmondOrderService.Object,
            _mockSysmondInvoiceService.Object,
            _mockStockService.Object,
            _mockUow.Object,
            _mockMapper.Object
        );
    }

    [Fact]
    public async Task CreateAndConfirmInvoiceAsync_Should_Create_Invoice_Confirm_Order_And_Sync_With_Sysmond_When_Valid()
    {
        var sysmondOrderId = Guid.NewGuid();
        var sysmondInvoiceId = Guid.NewGuid();
        var orderItems = new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 2, UnitPrice = 100, LineTotal = 200 } };
        var order = new Order { Id = 1, SellerId = 10, CustomerId = 5, Status = OrderStatus.Pending, TotalAmount = 200, OrderItems = orderItems, SysmondOrderId = sysmondOrderId };
        var customer = new Customer { Id = 5, FirstName = "Ahmet", LastName = "Yılmaz" };
        var product = new Product { Id = 1, Name = "Laptop", SysmondStockId = Guid.NewGuid() };

        _mockOrderRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<Expression<Func<Order, object?>>[]>())).ReturnsAsync(order);
        _mockCustomerRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(customer);
        _mockProductRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);

        _mockSysmondOrderService.Setup(s => s.UpdateOrderStatusAsync(It.IsAny<SysmondOrderStatusUpdateDto>())).Returns(Task.CompletedTask);
        _mockSysmondInvoiceService.Setup(s => s.CreateDraftInvoiceAsync(It.IsAny<SysmondInvoiceDraftCreateDto>())).ReturnsAsync(sysmondInvoiceId);
        _mockSysmondInvoiceService.Setup(s => s.AddInvoiceItemAsync(It.IsAny<SysmondInvoiceItemCreateDto>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<InvoiceDto>(It.IsAny<Invoice>())).Returns(new InvoiceDto { Items = new List<InvoiceItemDto>() });

        await _sellerOrderService.CreateAndConfirmInvoiceAsync(1, 10);

        _mockSysmondOrderService.Verify(s => s.UpdateOrderStatusAsync(It.Is<SysmondOrderStatusUpdateDto>(dto => dto.Status == 20)), Times.Once);
        _mockSysmondInvoiceService.Verify(s => s.CreateDraftInvoiceAsync(It.IsAny<SysmondInvoiceDraftCreateDto>()), Times.Once);
        _mockSysmondInvoiceService.Verify(s => s.AddInvoiceItemAsync(It.IsAny<SysmondInvoiceItemCreateDto>()), Times.Once);

        _mockInvoiceRepo.Verify(r => r.AddAsync(It.Is<Invoice>(i => i.OrderId == 1 && i.SellerId == 10 && i.Status == InvoiceStatus.Confirmed)), Times.Once);
        _mockOrderRepo.Verify(r => r.Update(It.Is<Order>(o => o.Status == OrderStatus.Invoiced)), Times.Once);
        _mockUow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ShipOrderAsync_Should_Update_Status_To_Shipped_And_Sync_With_Sysmond_As_PartiallyDelivered()
    {
        var order = new Order { Id = 1, SellerId = 10, Status = OrderStatus.Invoiced, SysmondOrderId = Guid.NewGuid() };
        _mockOrderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _mockSysmondOrderService.Setup(s => s.UpdateOrderStatusAsync(It.IsAny<SysmondOrderStatusUpdateDto>())).Returns(Task.CompletedTask);

        await _sellerOrderService.ShipOrderAsync(1, 10);

        _mockSysmondOrderService.Verify(s => s.UpdateOrderStatusAsync(It.Is<SysmondOrderStatusUpdateDto>(dto => dto.Status == 30)), Times.Once);
        order.Status.Should().Be(OrderStatus.Shipped);
        _mockOrderRepo.Verify(r => r.Update(order), Times.Once);
        _mockUow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetPendingOrdersAsync_Should_Update_Status_And_Exclude_Approved_Sysmond_Orders()
    {
        var sysmondGuid = Guid.NewGuid();
        var order = new Order { Id = 1, SellerId = 1, CustomerId = 1, Status = OrderStatus.Pending, SysmondOrderId = sysmondGuid, OrderItems = new List<OrderItem>() };

        _mockOrderRepo.Setup(r => r.GetWhereAsync(It.IsAny<Expression<Func<Order, bool>>>(), It.IsAny<Expression<Func<Order, object>>[]>()))
            .ReturnsAsync(new List<Order> { order });

        _mockSysmondOrderService.Setup(s => s.GetOrderStatusesByIdsAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(new List<SysmondOrderDto> { new SysmondOrderDto { Id = sysmondGuid, Status = 20 } });

        var result = await _sellerOrderService.GetPendingOrdersAsync(1);

        order.Status.Should().Be(OrderStatus.Invoiced);
        _mockOrderRepo.Verify(r => r.Update(order), Times.Once);
        _mockUow.Verify(u => u.SaveChangesAsync(), Times.Once);
        result.Should().BeEmpty();
    }
}