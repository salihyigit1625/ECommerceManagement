using AutoMapper;
using ECommerceManagement.Domain.Constants;
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
    private readonly IGenericRepository<UserRole> _userRoleRepository;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthService(
        IGenericRepository<User> userRepository,
        IGenericRepository<Customer> customerRepository,
        IGenericRepository<Seller> sellerRepository,
        IGenericRepository<UserRole> userRoleRepository,
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _customerRepository = customerRepository;
        _sellerRepository = sellerRepository;
        _userRoleRepository = userRoleRepository;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<TokenResponseDto> RegisterCustomerAsync(CustomerRegisterDto dto)
    {
        var existingUsers = await _userRepository.GetAllAsync();
        if (existingUsers.Any(u => u.Email == dto.Email))
            throw new InvalidOperationException("Bu e-posta adresi ile zaten bir kayıt mevcut.");

        var user = _mapper.Map<User>(dto);
        user.CreatedAt = DateTime.UtcNow;
        user.IsActive = true;
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync(); 

        var customer = _mapper.Map<Customer>(dto);
        customer.UserId = user.Id;
        customer.CreatedAt = DateTime.UtcNow;
        await _customerRepository.AddAsync(customer);

        // Kullanıcıya Veritabanında Rol Atama (Id: 4 -> Customer)
        var userRole = new UserRole { UserId = user.Id, RoleId = 4 };
        await _userRoleRepository.AddAsync(userRole);
        
        await _unitOfWork.SaveChangesAsync();

        return await GenerateTokensForUserAsync(user, AppRoles.Customer);
    }

    public async Task<TokenResponseDto> RegisterSellerAsync(SellerRegisterDto dto)
    {
        var existingUsers = await _userRepository.GetAllAsync();
        if (existingUsers.Any(u => u.Email == dto.Email))
            throw new InvalidOperationException("Bu e-posta adresi ile zaten bir kayıt mevcut.");

        var user = _mapper.Map<User>(dto);
        user.CreatedAt = DateTime.UtcNow;
        user.IsActive = true;
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync(); 

        var seller = _mapper.Map<Seller>(dto);
        seller.UserId = user.Id;
        seller.CreatedAt = DateTime.UtcNow;
        await _sellerRepository.AddAsync(seller);

        // Kullanıcıya Veritabanında Rol Atama (Id: 3 -> Seller)
        var userRole = new UserRole { UserId = user.Id, RoleId = 3 };
        await _userRoleRepository.AddAsync(userRole);
        
        await _unitOfWork.SaveChangesAsync();

        return await GenerateTokensForUserAsync(user, AppRoles.Seller);
    }

    public async Task<TokenResponseDto> LoginAsync(LoginDto dto)
    {
        var users = await _userRepository.GetAllAsync();
        var user = users.FirstOrDefault(u => u.Email == dto.Email);

        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Geçersiz e-posta veya şifre.");

        if (user.PasswordHash != dto.Password)
        {
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                throw new UnauthorizedAccessException("Geçersiz e-posta veya şifre.");
        }

        string role;
        
        if (user.Email == "superadmin@sistem.com") 
        {
            role = AppRoles.SuperAdmin;
        }
        else if (user.Email == "admin@sistem.com") 
        {
            role = AppRoles.Admin;
        }
        else 
        {
            var sellers = await _sellerRepository.GetAllAsync();
            var isSeller = sellers.Any(s => s.UserId == user.Id);
            role = isSeller ? AppRoles.Seller : AppRoles.Customer;
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
        
        if (user.Email == "superadmin@sistem.com") 
        {
            role = AppRoles.SuperAdmin;
        }
        else if (user.Email == "admin@sistem.com") 
        {
            role = AppRoles.Admin;
        }
        else 
        {
            var sellers = await _sellerRepository.GetAllAsync();
            var isSeller = sellers.Any(s => s.UserId == user.Id);
            role = isSeller ? AppRoles.Seller : AppRoles.Customer;
        }

        return await GenerateTokensForUserAsync(user, role);
    }

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