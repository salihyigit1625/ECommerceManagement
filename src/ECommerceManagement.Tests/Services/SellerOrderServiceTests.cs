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
    private readonly Mock<ISysmondOrderService> _mockSysmondOrderService; // Sysmond mock nesnesi eklendi
    private readonly Mock<ISysmondStockService> _mockStockService;
    private readonly Mock<ISysmondInvoiceService>  _mockSysmondInvoiceService;
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
        _mockSysmondOrderService = new Mock<ISysmondOrderService>(); // Örneklendi
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
    public async Task CreateInvoiceDraftAsync_Should_Throw_KeyNotFoundException_When_Order_Belongs_To_Another_Seller()
    {
        var order = new Order { Id = 1, SellerId = 99, Status = OrderStatus.Pending };
        
        _mockOrderRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<Expression<Func<Order, object?>>[]>())).ReturnsAsync(order);

        Func<Task> act = async () => await _sellerOrderService.CreateAndConfirmInvoiceAsync(1, 10);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Sipariş bulunamadı veya bu satıcıya ait değil.");
    }

    [Fact]
    public async Task ShipOrderAsync_Should_Throw_KeyNotFoundException_When_Order_Belongs_To_Another_Seller()
    {
        var order = new Order { Id = 1, SellerId = 99, Status = OrderStatus.Invoiced };
        _mockOrderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        Func<Task> act = async () => await _sellerOrderService.ShipOrderAsync(1, 10);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Sipariş bulunamadı.");
    }

    [Fact]
    public async Task CreateInvoiceDraftAsync_Should_Create_Invoice_With_Items_When_Valid()
    {
        var orderItems = new List<OrderItem>
        {
            new OrderItem { ProductId = 1, Quantity = 2, UnitPrice = 100, LineTotal = 200 }
        };
        var order = new Order { Id = 1, SellerId = 10, CustomerId = 5, Status = OrderStatus.Pending, TotalAmount = 200, OrderItems = orderItems };
        var customer = new Customer { Id = 5, FirstName = "Ahmet", LastName = "Yılmaz" };

        _mockOrderRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<Expression<Func<Order, object?>>[]>())).ReturnsAsync(order);
        _mockCustomerRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(customer);
        _mockMapper.Setup(m => m.Map<InvoiceDto>(It.IsAny<Invoice>())).Returns(new InvoiceDto());

        await _sellerOrderService.CreateAndConfirmInvoiceAsync(1, 10);

        _mockInvoiceRepo.Verify(r => r.AddAsync(It.Is<Invoice>(i =>
            i.OrderId == 1 &&
            i.SellerId == 10 &&
            i.InvoiceItems.Count == 1 &&
            i.InvoiceItems.First().ProductId == 1 &&
            i.InvoiceItems.First().LineTotal == 200
        )), Times.Once);
        _mockUow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CancelOrderAsync_Should_Revert_Stock_Log_Entry_Movement_And_Cancel_Invoice()
    {
        var orderItems = new List<OrderItem>
        {
            new OrderItem { ProductId = 1, Quantity = 5 }
        };
        var order = new Order { Id = 1, SellerId = 10, Status = OrderStatus.Pending, OrderItems = orderItems };
        var product = new Product { Id = 1, Quantity = 15 };
        var invoice = new Invoice { Id = 1, OrderId = 1, Status = InvoiceStatus.Waiting };

        _mockOrderRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<Expression<Func<Order, object?>>[]>())).ReturnsAsync(order);
        _mockProductRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _mockInvoiceRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Invoice, bool>>>(), It.IsAny<Expression<Func<Invoice, object>>[]>())).ReturnsAsync(invoice);

        await _sellerOrderService.CancelOrderAsync(1, 10);

        order.Status.Should().Be(OrderStatus.Canceled);
        _mockOrderRepo.Verify(r => r.Update(order), Times.Once);

        _mockProductRepo.Verify(r => r.Update(It.Is<Product>(p => p.Quantity == 20)), Times.Once);

        _mockMovementRepo.Verify(m => m.AddAsync(It.Is<ProductMovement>(pm =>
            pm.ProductId == 1 &&
            pm.MovementType == MovementType.Entry &&
            pm.Quantity == 5 &&
            pm.ReferenceId == 1
        )), Times.Once);

        invoice.Status.Should().Be(InvoiceStatus.Canceled);
        _mockInvoiceRepo.Verify(r => r.Update(invoice), Times.Once);

        _mockUow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetPendingOrdersAsync_Should_Update_Status_And_Exclude_Approved_Sysmond_Orders()
    {
        // 1. Arrange
        var sysmondGuid = Guid.NewGuid();
        var orderItems = new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 1, UnitPrice = 100, LineTotal = 100 } };
        
        var order = new Order
        {
            Id = 1,
            SellerId = 1,
            CustomerId = 1,
            Status = OrderStatus.Pending,
            SysmondOrderId = sysmondGuid,
            OrderItems = orderItems
        };

        _mockOrderRepo.Setup(r => r.GetWhereAsync(
            It.IsAny<Expression<Func<Order, bool>>>(),
            It.IsAny<Expression<Func<Order, object>>[]>()))
            .ReturnsAsync(new List<Order> { order });

        _mockProductRepo.Setup(r => r.GetWhereAsync(
            It.IsAny<Expression<Func<Product, bool>>>(),
            It.IsAny<Expression<Func<Product, object>>[]>()))
            .ReturnsAsync(new List<Product> { new Product { Id = 1, Name = "Test Ürün" } });

        // Sysmond tarafında siparişin onaylandığını (Status: 20) taklit ediyoruz
        _mockSysmondOrderService.Setup(s => s.GetOrderStatusesByIdsAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(new List<SysmondOrderDto>
            {
                new SysmondOrderDto { Id = sysmondGuid, Status = 20 }
            });

        // 2. Act
        var result = await _sellerOrderService.GetPendingOrdersAsync(1);

        // 3. Assert
        // Sipariş Sysmond'da onaylandığı için lokalde Invoiced olmalı ve bekleyenler listesinden çıkarılmalı
        order.Status.Should().Be(OrderStatus.Invoiced);
        _mockOrderRepo.Verify(r => r.Update(order), Times.Once);
        _mockUow.Verify(u => u.SaveChangesAsync(), Times.Once);
        result.Should().BeEmpty();
    }
}