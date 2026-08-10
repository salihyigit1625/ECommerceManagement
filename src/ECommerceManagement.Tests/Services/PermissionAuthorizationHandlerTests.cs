using System.Security.Claims;
using ECommerceManagement.Domain.Constants;
using ECommerceManagement.Infrastructure.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ECommerceManagement.Tests.Security;

public class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task HandleRequirementAsync_Should_Succeed_Immediately_If_User_Is_SuperAdmin()
    {
        // 1. Arrange (Hazırlık)
        // Erişilmek istenen rastgele bir yetki kilit noktası oluşturuyoruz
        var requirement = new PermissionRequirement(AppPermissions.ManageCatalog);
        
        // Cüzdanında "SuperAdmin" rolü olan sahte bir kullanıcı oluşturuyoruz
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, AppRoles.SuperAdmin)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType"); // "TestAuthType" login olduğunu belirtir
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var context = new AuthorizationHandlerContext(
            new[] { requirement }, 
            claimsPrincipal, 
            null);

        // Sistemin veritabanına veya Redis'e bağlanmak için kullanacağı Scope'u sahte (mock) olarak veriyoruz.
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        var handler = new PermissionAuthorizationHandler(mockScopeFactory.Object);

        // 2. Act (Eylem - Kapıdaki güvenliğe kimliği göster)
        await handler.HandleAsync(context);

        // 3. Assert (Doğrulama)
        
        // KRİTİK DOĞRULAMA 1: Kapı açıldı mı? (İşlem başarılı oldu mu?)
        context.HasSucceeded.Should().BeTrue();
        
        // KRİTİK DOĞRULAMA 2: Güvenlik görevlisi Süper Admin'i görünce veritabanına veya Redis'e (Scope) HİÇ BAŞVURMADI, değil mi?
        // Times.Never diyerek arka tarafa kesinlikle sorgu atılmadığını kanıtlıyoruz.
        mockScopeFactory.Verify(x => x.CreateScope(), Times.Never);
    }
}