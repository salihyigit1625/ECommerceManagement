using ECommerceManagement.Application.DTOs.Orders;
using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Application.Services;
using ECommerceManagement.Domain.Entities;
using Moq;
using Xunit;

namespace ECommerceManagement.Tests;

public class CustomerOrderServiceTests
{
    private readonly Mock<IGenericRepository<Order>> _mockOrderRepo;
    private readonly Mock<IGenericRepository<Product>> _mockProductRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly CustomerOrderService _customerOrderService;

    public CustomerOrderServiceTests()
    {
        // 1. Sahte (Mock) nesnelerimizi üretiyoruz
        _mockOrderRepo = new Mock<IGenericRepository<Order>>();
        _mockProductRepo = new Mock<IGenericRepository<Product>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        // 2. Servisimizi bu sahte nesnelerle ayağa kaldırıyoruz
        _customerOrderService = new CustomerOrderService(
            _mockOrderRepo.Object,
            _mockProductRepo.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldThrowException_WhenRequestedQuantityExceedsStock()
    {
        // ==========================================
        // ARRANGE (Hazırlık - Veritabanı yerine sahte veri döneceğiz)
        // ==========================================
        var fakeProductId = 1;
        var fakeProducts = new List<Product>
        {
            new Product 
            { 
                Id = fakeProductId, 
                Name = "Oyuncu Klavyesi", 
                Quantity = 5, // DİKKAT: Stokta sadece 5 tane var!
                IsActive = true 
            }
        };

        // Servis DB'den GetAllAsync çağırdığında ona SQL'e gitme, benim fake listemi dön diyoruz
        _mockProductRepo.Setup(repo => repo.GetAllAsync()).ReturnsAsync(fakeProducts);

        var createOrderDto = new CreateOrderDto
        {
            CustomerId = 1,
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto 
                { 
                    ProductId = fakeProductId, 
                    Quantity = 10 // DİKKAT: Müşteri 10 tane istiyor (Stok aşımı!)
                }
            }
        };

        // ==========================================
        // ACT & ASSERT (Eylem ve Doğrulama)
        // ==========================================
        
        // Servisin hata (Exception) fırlatmasını bekliyoruz
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _customerOrderService.CreateOrderAsync(createOrderDto));

        // Fırlatılan hatanın mesajı doğru mu diye kontrol ediyoruz
        Assert.Contains("yeterli stok yok", exception.Message);
        
        // Ekstra Güvenlik Kontrolü: Hata fırladığı için DB'ye hiçbir kayıt gitmemiş olmalı!
        _mockOrderRepo.Verify(repo => repo.AddAsync(It.IsAny<Order>()), Times.Never);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Never);
    }
    
    
    [Fact]
    public async Task CreateOrderAsync_ShouldCreateOrder_WhenStockIsSufficient()
    {
        // ==========================================
        // ARRANGE (Hazırlık)
        // ==========================================
        var fakeProductId = 1;
        var fakeProducts = new List<Product>
        {
            new Product 
            { 
                Id = fakeProductId, 
                Name = "Oyuncu Klavyesi", 
                Quantity = 10, // DİKKAT: Stokta 10 tane var
                Price = 2500,
                SellerId = 1,
                IsActive = true 
            }
        };

        _mockProductRepo.Setup(repo => repo.GetAllAsync()).ReturnsAsync(fakeProducts);

        var createOrderDto = new CreateOrderDto
        {
            CustomerId = 1,
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto 
                { 
                    ProductId = fakeProductId, 
                    Quantity = 2 // Müşteri 2 adet istiyor, stok yeterli.
                }
            }
        };

        // ==========================================
        // ACT (Eylem)
        // ==========================================
        
        // Hata fırlatmadan çalışmasını bekliyoruz
        await _customerOrderService.CreateOrderAsync(createOrderDto);

        // ==========================================
        // ASSERT (Doğrulama)
        // ==========================================
        
        // 1. Memory'deki stok 10'dan 8'e düşmüş mü?
        Assert.Equal(8, fakeProducts[0].Quantity);
        
        // 2. Product repository'sinin Update metodu 1 kez çağrılmış mı?
        _mockProductRepo.Verify(repo => repo.Update(It.IsAny<Product>()), Times.Once);

        // 3. Order repository'sinin AddAsync metodu (Siparişi kaydetmek için) 1 kez çağrılmış mı?
        _mockOrderRepo.Verify(repo => repo.AddAsync(It.IsAny<Order>()), Times.Once);

        // 4. Tüm bu işlemler Unit of Work ile commit edilmiş mi?
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
    }
}