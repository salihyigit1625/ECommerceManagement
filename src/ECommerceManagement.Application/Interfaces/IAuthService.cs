using ECommerceManagement.Application.DTOs.Auth;

namespace ECommerceManagement.Application.Interfaces
{
    public interface IAuthService
    {
        Task<TokenResponseDto> RegisterCustomerAsync(CustomerRegisterDto dto);
        Task<TokenResponseDto> RegisterSellerAsync(SellerRegisterDto dto);
        Task<TokenResponseDto> LoginAsync(LoginDto dto);
        Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto);
    }
}