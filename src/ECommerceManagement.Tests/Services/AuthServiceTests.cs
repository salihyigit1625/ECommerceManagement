using ECommerceManagement.Application.DTOs.Auth;
using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Application.Services;
using ECommerceManagement.Domain.Constants;
using ECommerceManagement.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace ECommerceManagement.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IGenericRepository<User>> _mockUserRepo;
    private readonly Mock<IGenericRepository<Customer>> _mockCustomerRepo;
    private readonly Mock<IGenericRepository<Seller>> _mockSellerRepo;
    private readonly Mock<IGenericRepository<UserRole>> _mockUserRoleRepo;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // Bağımlılıkların (Veritabanı, Token Servisi vb.) sahtelerini (mock) üretiyoruz.
        _mockUserRepo = new Mock<IGenericRepository<User>>();
        _mockCustomerRepo = new Mock<IGenericRepository<Customer>>();
        _mockSellerRepo = new Mock<IGenericRepository<Seller>>();
        _mockUserRoleRepo = new Mock<IGenericRepository<UserRole>>();
        _mockTokenService = new Mock<ITokenService>();
        _mockUow = new Mock<IUnitOfWork>();

        _authService = new AuthService(
            _mockUserRepo.Object,
            _mockCustomerRepo.Object,
            _mockSellerRepo.Object,
            _mockUserRoleRepo.Object,
            _mockTokenService.Object,
            _mockUow.Object
        );
    }

    [Fact]
    public async Task RegisterCustomer_Should_HashPassword_Assign_CustomerRole_And_Return_Tokens()
    {
        // 1. Arrange (Hazırlık)
        var dto = new CustomerRegisterDto
        {
            Username = "newcustomer",
            Email = "customer@test.com",
            Password = "StrongPassword123!",
            FirstName = "Test",
            LastName = "Customer"
        };

        // Veritabanında bu maile sahip başka biri yokmuş gibi davran
        _mockUserRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<User>());

        // Token servisi token ürettiğinde ne döneceğini söylüyoruz
        _mockTokenService.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IList<string>>()))
            .Returns("mock_access_token");
        _mockTokenService.Setup(x => x.GenerateRefreshToken())
            .Returns("mock_refresh_token");

        // 2. Act (Eylem - Servisi Çalıştır)
        var result = await _authService.RegisterCustomerAsync(dto);

        // 3. Assert (Doğrulamalar)
        
        // Token'lar başarıyla döndü mü?
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("mock_access_token");
        result.RefreshToken.Should().Be("mock_refresh_token");

        // KRİTİK DOĞRULAMA 1: Kullanıcı eklendi mi ve şifresi hash'lendi mi? (Açık metin olarak kaydedilmemeli)
        _mockUserRepo.Verify(x => x.AddAsync(It.Is<User>(u => 
            u.Email == dto.Email && 
            u.Username == dto.Username && 
            u.PasswordHash != dto.Password)), Times.Once); 

        // KRİTİK DOĞRULAMA 2: Yeni kullanıcıya Customer rolü (RoleId = 4) atandı mı?
        _mockUserRoleRepo.Verify(x => x.AddAsync(It.Is<UserRole>(ur => ur.RoleId == 4)), Times.Once);

        // KRİTİK DOĞRULAMA 3: İşlemler veritabanına kaydedildi mi?
        _mockUow.Verify(x => x.SaveChangesAsync(), Times.AtLeastOnce);
    }
    
    [Fact]
    public async Task LoginAsync_Should_Throw_UnauthorizedException_When_Password_Is_Wrong()
    {
        // 1. Arrange (Hazırlık)
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
        var dto = new LoginDto { Email = "wrong@test.com", Password = "WrongPassword123!" };
        
        var user = new User
        {
            Id = 1,
            Email = "wrong@test.com",
            IsActive = true
        };
        // Hata almamak için gerçekten Base64 formatında geçerli bir hash oluşturuyoruz
        user.PasswordHash = hasher.HashPassword(user, "RealPassword123!");

        _mockUserRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<User> { user });

        // 2. Act & 3. Assert (Eylem ve Doğrulama)
        // Hatalı şifre girildiğinde sistemin "UnauthorizedAccessException" fırlatmasını bekliyoruz.
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(dto));
        exception.Message.Should().Be("Geçersiz e-posta veya şifre.");
    }

    [Fact]
    public async Task LoginAsync_Should_Return_Tokens_And_Assign_Seller_Role_For_Valid_Seller()
    {
        // 1. Arrange (Hazırlık)
        var dto = new LoginDto { Email = "seller@test.com", Password = "CorrectPassword123!" };
        var user = new User
        {
            Id = 2,
            Email = "seller@test.com",
            IsActive = true,
            // Şifreyi test ortamı için bypass edip direkt aynı veriyoruz
            PasswordHash = "CorrectPassword123!" 
        };

        // Bu kullanıcının bir Satıcı (Seller) profilinin olduğunu simüle ediyoruz
        var seller = new Seller { UserId = 2 };

        _mockUserRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<User> { user });
        _mockSellerRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Seller> { seller });
        
        // Token üretilirken rol listesinin içinde "Seller" olmasını şart koşuyoruz
        _mockTokenService.Setup(x => x.GenerateAccessToken(user, It.Is<List<string>>(roles => roles.Contains(AppRoles.Seller))))
            .Returns("seller_access_token");
        _mockTokenService.Setup(x => x.GenerateRefreshToken())
            .Returns("seller_refresh_token");

        // 2. Act (Eylem)
        var result = await _authService.LoginAsync(dto);

        // 3. Assert (Doğrulama)
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("seller_access_token");
        result.RefreshToken.Should().Be("seller_refresh_token");
        
        // Üretilen Refresh Token veritabanına kaydedilmek üzere güncellenmiş mi?
        _mockUserRepo.Verify(x => x.Update(It.Is<User>(u => u.RefreshToken == "seller_refresh_token")), Times.Once);
        _mockUow.Verify(x => x.SaveChangesAsync(), Times.AtLeastOnce);
    }
    
    
    
}