using ECommerceManagement.Application.Constants;
using ECommerceManagement.Application.DTOs.Auth;
using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace ECommerceManagement.Application.Services;

public class AuthService : IAuthService
{
    private readonly IGenericRepository<User> _userRepository;
    private readonly IGenericRepository<Customer> _customerRepository;
    private readonly IGenericRepository<Seller> _sellerRepository;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthService(
        IGenericRepository<User> userRepository,
        IGenericRepository<Customer> customerRepository,
        IGenericRepository<Seller> sellerRepository,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _customerRepository = customerRepository;
        _sellerRepository = sellerRepository;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
    }

    // ==========================================
    // 1. MÜŞTERİ KAYIT (Sadece Müşteri Bilgileri)
    // ==========================================
    public async Task<TokenResponseDto> RegisterCustomerAsync(CustomerRegisterDto dto)
    {
        var existingUsers = await _userRepository.GetAllAsync();
        if (existingUsers.Any(u => u.Email == dto.Email))
            throw new InvalidOperationException("Bu e-posta adresi ile zaten bir kayıt mevcut.");

        var user = new User { Username = dto.Username, Email = dto.Email, CreatedAt = DateTime.UtcNow, IsActive = true };
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync(); 

        var customer = new Customer
        {
            UserId = user.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            CreatedAt = DateTime.UtcNow
        };
        await _customerRepository.AddAsync(customer);
        await _unitOfWork.SaveChangesAsync();

        return await GenerateTokensForUserAsync(user, Roles.Customer);
    }

    // ==========================================
    // 2. SATICI KAYIT (Sadece Şirket Bilgileri)
    // ==========================================
    public async Task<TokenResponseDto> RegisterSellerAsync(SellerRegisterDto dto)
    {
        var existingUsers = await _userRepository.GetAllAsync();
        if (existingUsers.Any(u => u.Email == dto.Email))
            throw new InvalidOperationException("Bu e-posta adresi ile zaten bir kayıt mevcut.");

        var user = new User { Username = dto.Username, Email = dto.Email, CreatedAt = DateTime.UtcNow, IsActive = true };
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync(); 

        var seller = new Seller
        {
            UserId = user.Id,
            CompanyName = dto.CompanyName,
            TaxNumber = dto.TaxNumber,
            ContactEmail = dto.Email,
            CreatedAt = DateTime.UtcNow
        };
        await _sellerRepository.AddAsync(seller);
        await _unitOfWork.SaveChangesAsync();

        return await GenerateTokensForUserAsync(user, Roles.Seller);
    }

    // ==========================================
    // 3. ORTAK LOGİN (Admin, Seller, Customer)
    // ==========================================
    public async Task<TokenResponseDto> LoginAsync(LoginDto dto)
    {
        var users = await _userRepository.GetAllAsync();
        var user = users.FirstOrDefault(u => u.Email == dto.Email);

        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Geçersiz e-posta veya şifre.");

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Geçersiz e-posta veya şifre.");

        string role;
        
        // ADMIN KONTROLÜ (Admin dışarıdan kayıt olamaz. DB'ye "admin@sistem.com" eklersen direkt Admin olarak tanır)
        if (user.Email == "admin@sistem.com") 
        {
            role = Roles.Admin;
        }
        else 
        {
            var sellers = await _sellerRepository.GetAllAsync();
            var isSeller = sellers.Any(s => s.UserId == user.Id);
            role = isSeller ? Roles.Seller : Roles.Customer;
        }

        return await GenerateTokensForUserAsync(user, role);
    }

    public async Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        var users = await _userRepository.GetAllAsync();
        var user = users.FirstOrDefault(u => u.RefreshToken == dto.RefreshToken);

        if (user == null || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş Refresh Token. Lütfen tekrar giriş yapın.");

        string role;
        if (user.Email == "admin@sistem.com") role = Roles.Admin;
        else 
        {
            var sellers = await _sellerRepository.GetAllAsync();
            var isSeller = sellers.Any(s => s.UserId == user.Id);
            role = isSeller ? Roles.Seller : Roles.Customer;
        }

        return await GenerateTokensForUserAsync(user, role);
    }

    // Kod tekrarını önlemek için Token üreten ortak yardımcı metot (Private)
    private async Task<TokenResponseDto> GenerateTokensForUserAsync(User user, string role)
    {
        var accessToken = _tokenService.GenerateAccessToken(user, new List<string> { role });
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(60),
            RefreshTokenExpiration = DateTime.UtcNow.AddDays(7)
        };
    }
}