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

    public async Task<TokenResponseDto> RegisterAsync(RegisterDto dto)
    {
        // 1. Email kontrolü
        var existingUsers = await _userRepository.GetAllAsync();
        if (existingUsers.Any(u => u.Email == dto.Email))
            throw new InvalidOperationException("Bu e-posta adresi ile zaten bir kayıt mevcut.");

        // 2. Yeni User nesnesi oluşturma
        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync(); // User ID oluşması için kaydediyoruz

        // 3. UserType'a göre Profil (Customer veya Seller) oluşturma
        if (dto.UserType.Equals("Seller", StringComparison.OrdinalIgnoreCase))
        {
            var seller = new Seller
            {
                UserId = user.Id,
                CompanyName = dto.CompanyName ?? "Şirket Adı Belirtilmedi",
                TaxNumber = dto.TaxNumber ?? $"TAX-{Guid.NewGuid().ToString().Substring(0, 8)}",
                ContactEmail = dto.Email,
                CreatedAt = DateTime.UtcNow
            };
            await _sellerRepository.AddAsync(seller);
        }
        else
        {
            var customer = new Customer
            {
                UserId = user.Id,
                FirstName = dto.FirstName ?? "İsimsiz",
                LastName = dto.LastName ?? "Müşteri",
                CreatedAt = DateTime.UtcNow
            };
            await _customerRepository.AddAsync(customer);
        }

        await _unitOfWork.SaveChangesAsync();

        // 4. Token üret ve dön
        var roles = new List<string> { dto.UserType }; // Örn: "Customer" veya "Seller"
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Refresh token'ı kullanıcıya kaydedelim
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

    public async Task<TokenResponseDto> LoginAsync(LoginDto dto)
    {
        var users = await _userRepository.GetAllAsync();
        var user = users.FirstOrDefault(u => u.Email == dto.Email);

        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Geçersiz e-posta veya şifre.");

        // Şifre doğrulama
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Geçersiz e-posta veya şifre.");

        // Şimdilik rollerini varsayılan olarak user türüne göre atayabiliriz (İleride Role tablosundan çekeceğiz)
        // Kullanıcının Seller mi Customer mı olduğunu tablolardan sorgulayalım:
        var sellers = await _sellerRepository.GetAllAsync();
        var isSeller = sellers.Any(s => s.UserId == user.Id);
        var role = isSeller ? "Seller" : "Customer";

        var roles = new List<string> { role };
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
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