using ECommerceManagement.Application.DTOs.Auth;
using ECommerceManagement.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceManagement.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register-customer")]
        public async Task<IActionResult> RegisterCustomer([FromBody] CustomerRegisterDto dto)
        {
            var result = await _authService.RegisterCustomerAsync(dto);
            return Ok(result);
        }

        [HttpPost("register-seller")]
        public async Task<IActionResult> RegisterSeller([FromBody] SellerRegisterDto dto)
        {
            var result = await _authService.RegisterSellerAsync(dto);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            var result = await _authService.RefreshTokenAsync(dto);
            return Ok(result);
        }
    }
}