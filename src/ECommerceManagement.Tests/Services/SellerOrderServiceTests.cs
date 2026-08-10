using AutoMapper;
using ECommerceManagement.Application.DTOs.Invoices;
using ECommerceManagement.Application.Services;
using ECommerceManagement.Domain.Entities;
using ECommerceManagement.Domain.Enums;
using ECommerceManagement.Application.Interfaces;
using FluentAssertions;
using Moq;

namespace ECommerceManagement.Tests.Services;

public class SellerOrderServiceTests
{
    private readonly Mock<IGenericRepository<Order>> _mockOrderRepo;
    private readonly Mock<IGenericRepository<Invoice>> _mockInvoiceRepo;
    private readonly Mock<IGenericRepository<Customer>> _mockCustomerRepo;
    private readonly Mock<IGenericRepository<Product>> _mockProductRepo;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IMapper> _mockMapper;
    private readonly SellerOrderService _sellerOrderService;

    public SellerOrderServiceTests()
    {
        _mockOrderRepo = new Mock<IGenericRepository<Order>>();
        _mockInvoiceRepo = new Mock<IGenericRepository<Invoice>>();
        _mockCustomerRepo = new Mock<IGenericRepository<Customer>>();
        _mockProductRepo = new Mock<IGenericRepository<Product>>();
        _mockUow = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _sellerOrderService = new SellerOrderService(
            _mockOrderRepo.Object,
            _mockInvoiceRepo.Object,
            _mockCustomerRepo.Object,
            _mockProductRepo.Object,
            _mockUow.Object,
            _mockMapper.Object
        );
    }

    [Fact]
    public async Task CreateInvoiceDraftAsync_Should_Throw_KeyNotFoundException_When_Order_Belongs_To_Another_Seller()
    {
        // Arrange: Sipariş SellerId = 99'a ait
        var order = new Order { Id = 1, SellerId = 99, Status = OrderStatus.Pending };
        _mockOrderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        // Act: SellerId = 10 fatura kesmeye çalışıyor
        Func<Task> act = async () => await _sellerOrderService.CreateInvoiceDraftAsync(1, 10);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Sipariş bulunamadı veya bu satıcıya ait değil.");
    }

    [Fact]
    public async Task ShipOrderAsync_Should_Throw_KeyNotFoundException_When_Order_Belongs_To_Another_Seller()
    {
        // Arrange: Sipariş SellerId = 99'a ait
        var order = new Order { Id = 1, SellerId = 99, Status = OrderStatus.Invoiced };
        _mockOrderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        // Act: SellerId = 10 kargolamaya çalışıyor
        Func<Task> act = async () => await _sellerOrderService.ShipOrderAsync(1, 10);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Sipariş bulunamadı.");
    }
}