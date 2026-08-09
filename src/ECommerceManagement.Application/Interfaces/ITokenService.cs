using ECommerceManagement.Domain.Entities;

namespace ECommerceManagement.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user, IList<string> roles);
    string GenerateRefreshToken();
}