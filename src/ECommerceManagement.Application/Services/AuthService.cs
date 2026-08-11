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
    private readonly IGenericRepository<Role> _roleRepository;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthService(
        IGenericRepository<User> userRepository,
        IGenericRepository<Customer> customerRepository,
        IGenericRepository<Seller> sellerRepository,
        IGenericRepository<UserRole> userRoleRepository,
        IGenericRepository<Role> roleRepository,
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _customerRepository = customerRepository;
        _sellerRepository = sellerRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<TokenResponseDto> RegisterCustomerAsync(CustomerRegisterDto dto)
    {
        var existingUser = await _userRepository.GetAsync(u => u.Email == dto.Email);
        if (existingUser != null)
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

        var userRole = new UserRole { UserId = user.Id, RoleId = 4 };
        await _userRoleRepository.AddAsync(userRole);
        
        await _unitOfWork.SaveChangesAsync();

        return await GenerateTokensForUserAsync(user, new List<string> { AppRoles.Customer });
    }

    public async Task<TokenResponseDto> RegisterSellerAsync(SellerRegisterDto dto)
    {
        var existingUser = await _userRepository.GetAsync(u => u.Email == dto.Email);
        if (existingUser != null)
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

        var userRole = new UserRole { UserId = user.Id, RoleId = 3 };
        await _userRoleRepository.AddAsync(userRole);
        
        await _unitOfWork.SaveChangesAsync();

        return await GenerateTokensForUserAsync(user, new List<string> { AppRoles.Seller });
    }

    public async Task<TokenResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetAsync(u => u.Email == dto.Email);

        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Geçersiz e-posta veya şifre.");

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Geçersiz e-posta veya şifre.");

        var userRoles = await _userRoleRepository.GetWhereAsync(ur => ur.UserId == user.Id);
        var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
        
        var roles = await _roleRepository.GetWhereAsync(r => roleIds.Contains(r.Id));
        var roleNames = roles.Select(r => r.Name).ToList();

        if (!roleNames.Any())
            throw new UnauthorizedAccessException("Kullanıcıya atanmış herhangi bir rol bulunamadı.");

        return await GenerateTokensForUserAsync(user, roleNames);
    }

    public async Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        var user = await _userRepository.GetAsync(u => u.RefreshToken == dto.RefreshToken);

        if (user == null || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş Refresh Token. Lütfen tekrar giriş yapın.");

        var userRoles = await _userRoleRepository.GetWhereAsync(ur => ur.UserId == user.Id);
        var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
        
        var roles = await _roleRepository.GetWhereAsync(r => roleIds.Contains(r.Id));
        var roleNames = roles.Select(r => r.Name).ToList();

        return await GenerateTokensForUserAsync(user, roleNames);
    }

    private async Task<TokenResponseDto> GenerateTokensForUserAsync(User user, IList<string> roles)
    {
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