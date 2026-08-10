using AutoMapper;
using ECommerceManagement.Application.DTOs.Auth;
using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Application.Mappings;
using ECommerceManagement.Application.Services;
using ECommerceManagement.Domain.Constants;
using ECommerceManagement.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IMapper _mapper;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockUserRepo = new Mock<IGenericRepository<User>>();
        _mockCustomerRepo = new Mock<IGenericRepository<Customer>>();
        _mockSellerRepo = new Mock<IGenericRepository<Seller>>();
        _mockUserRoleRepo = new Mock<IGenericRepository<UserRole>>();
        _mockTokenService = new Mock<ITokenService>();
        _mockUow = new Mock<IUnitOfWork>();

        var services = new ServiceCollection();
        services.AddLogging(); 
        services.AddAutoMapper(config => config.AddProfile<MappingProfile>());
        
        var serviceProvider = services.BuildServiceProvider();
        _mapper = serviceProvider.GetRequiredService<IMapper>();

        _authService = new AuthService(
            _mockUserRepo.Object,
            _mockCustomerRepo.Object,
            _mockSellerRepo.Object,
            _mockUserRoleRepo.Object,
            _mockTokenService.Object,
            _mockUow.Object,
            _mapper 
        );
    }

    [Fact]
    public async Task RegisterCustomer_Should_HashPassword_Assign_CustomerRole_And_Return_Tokens()
    {
        var dto = new CustomerRegisterDto
        {
            Username = "newcustomer",
            Email = "customer@test.com",
            Password = "StrongPassword123!",
            FirstName = "Test",
            LastName = "Customer"
        };

        _mockUserRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<User>());
        _mockTokenService.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IList<string>>())).Returns("mock_access_token");
        _mockTokenService.Setup(x => x.GenerateRefreshToken()).Returns("mock_refresh_token");

        var result = await _authService.RegisterCustomerAsync(dto);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("mock_access_token");
        
        _mockUserRepo.Verify(x => x.AddAsync(It.Is<User>(u => u.Email == dto.Email && u.PasswordHash != dto.Password)), Times.Once); 
        _mockUserRoleRepo.Verify(x => x.AddAsync(It.Is<UserRole>(ur => ur.RoleId == 4)), Times.Once);
        _mockUow.Verify(x => x.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task RegisterCustomerAsync_Should_Throw_InvalidOperationException_When_Email_Already_Exists()
    {
        // Arrange
        var dto = new CustomerRegisterDto { Email = "existing@test.com", Password = "Pass123!", Username = "user1" };
        var existingUser = new User { Email = "existing@test.com" };

        _mockUserRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<User> { existingUser });

        // Act
        Func<Task> act = async () => await _authService.RegisterCustomerAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Bu e-posta adresi ile zaten bir kayıt mevcut.");
    }

    [Fact]
    public async Task RegisterSellerAsync_Should_Create_Seller_Entity_And_Assign_SellerRole()
    {
        // Arrange
        var dto = new SellerRegisterDto
        {
            Username = "newseller",
            Email = "seller@company.com",
            Password = "StrongPassword123!",
            CompanyName = "Tekno A.Ş.",
            TaxNumber = "1234567890"
        };

        _mockUserRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<User>());
        _mockTokenService.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IList<string>>())).Returns("seller_token");
        _mockTokenService.Setup(x => x.GenerateRefreshToken()).Returns("seller_refresh");

        // Act
        var result = await _authService.RegisterSellerAsync(dto);

        // Assert
        result.Should().NotBeNull();
        _mockSellerRepo.Verify(x => x.AddAsync(It.Is<Seller>(s => s.CompanyName == "Tekno A.Ş.")), Times.Once);
        _mockUserRoleRepo.Verify(x => x.AddAsync(It.Is<UserRole>(ur => ur.RoleId == 3)), Times.Once); // Seller RoleId = 3
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_UnauthorizedException_When_User_Not_Found()
    {
        // Arrange
        var dto = new LoginDto { Email = "notfound@test.com", Password = "Password123!" };
        _mockUserRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<User>());

        // Act
        Func<Task> act = async () => await _authService.LoginAsync(dto);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Geçersiz e-posta veya şifre.");
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_UnauthorizedException_When_User_Is_Passive()
    {
        // Arrange
        var hasher = new PasswordHasher<User>();
        var dto = new LoginDto { Email = "banned@test.com", Password = "CorrectPassword123!" };
        var user = new User { Id = 1, Email = "banned@test.com", IsActive = false };
        user.PasswordHash = hasher.HashPassword(user, "CorrectPassword123!");

        _mockUserRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<User> { user });

        // Act
        Func<Task> act = async () => await _authService.LoginAsync(dto);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Geçersiz e-posta veya şifre.");
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_UnauthorizedException_When_Password_Is_Wrong()
    {
        var hasher = new PasswordHasher<User>();
        var dto = new LoginDto { Email = "wrong@test.com", Password = "WrongPassword123!" };
        var user = new User { Id = 1, Email = "wrong@test.com", IsActive = true };
        user.PasswordHash = hasher.HashPassword(user, "RealPassword123!");

        _mockUserRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<User> { user });

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(dto));
        exception.Message.Should().Be("Geçersiz e-posta veya şifre.");
    }
}