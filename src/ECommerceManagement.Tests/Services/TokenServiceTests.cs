using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ECommerceManagement.Domain.Entities;
using ECommerceManagement.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ECommerceManagement.Tests.Services;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        var jwtSettings = new Dictionary<string, string?>
        {
            {"JwtSettings:SecretKey", "SuperSecretKeyForTestingPurposeOnly12345!"},
            {"JwtSettings:Issuer", "TestIssuer"},
            {"JwtSettings:Audience", "TestAudience"},
            {"JwtSettings:AccessTokenExpirationMinutes", "60"}
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(jwtSettings)
            .Build();

        _tokenService = new TokenService(configuration);
    }

    [Fact]
    public void GenerateAccessToken_Should_Return_Valid_JwtToken_With_Correct_Claims()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@test.com"
        };
        var roles = new List<string> { "Customer", "Seller" };

        // Act
        var token = _tokenService.GenerateAccessToken(user, roles);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Ayarların doğruluğu
        jwtToken.Issuer.Should().Be("TestIssuer");
        jwtToken.Audiences.Should().Contain("TestAudience");

        // Kullanıcı bilgileri doğru basılmış mı?
        var claims = jwtToken.Claims.ToList();
        
        // ÇÖZÜM: .NET Token'ı oluştururken (Outbound) uzun URI'leri kısa isimlere çevirir.
        // Doğrudan token'ın içine basılan gerçek anahtarları arıyoruz.
        claims.Should().Contain(c => c.Type == "nameid" && c.Value == "1");
        claims.Should().Contain(c => c.Type == "unique_name" && c.Value == "testuser");
        claims.Should().Contain(c => c.Type == "email" && c.Value == "test@test.com");
        
        // Roller eksiksiz basılmış mı?
        var roleClaims = claims.Where(c => c.Type == "role").Select(c => c.Value).ToList();
        roleClaims.Should().Contain("Customer");
        roleClaims.Should().Contain("Seller");
    }

    [Fact]
    public void GenerateRefreshToken_Should_Return_Base64String()
    {
        // Act
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrWhiteSpace();
        
        Action act = () => Convert.FromBase64String(refreshToken);
        act.Should().NotThrow<FormatException>();
    }
}