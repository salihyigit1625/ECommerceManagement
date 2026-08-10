using System.Security.Claims;
using System.Text.Json;
using ECommerceManagement.Repository.Context; // DbContext'in olduğu namespace
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerceManagement.Infrastructure.Security
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public PermissionAuthorizationHandler(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            // 1. Kullanıcı doğrulanmamışsa direkt çık (401'e düşer)
            if (context.User.Identity == null || !context.User.Identity.IsAuthenticated)
                return;

            // 2. Token içinden Kullanıcı ID'sini al
            var userIdStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
                return;

            // DbContext ve Redis Cache servislerini Scope üzerinden çekiyoruz
            using var scope = _scopeFactory.CreateScope();
            var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

            string cacheKey = $"permissions_user_{userId}";
            List<string> userPermissions = new();

            // 3. Önce REDIS CACHE'e bakıyoruz (Veritabanını yormamak için)
            var cachedPermissions = await cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedPermissions))
            {
                // Cache'te varsa direkt al
                userPermissions = JsonSerializer.Deserialize<List<string>>(cachedPermissions) ?? new List<string>();
            }
            else
            {
                // Cache'te YOKSA veritabanından hesapla:
                
                // Adım A: Kullanıcının Rollerinden gelen tüm yetkileri çek
                var rolePermissions = await dbContext.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => rp.Permission.Name)
                    .ToListAsync();

                // Adım B: Kullanıcıya özel tanımlanmış yetki durumlarını (Override) çek
                var userOverrides = await dbContext.UserPermissions
                    .Include(up => up.Permission)
                    .Where(up => up.UserId == userId)
                    .ToListAsync();

                // Adım C: İzin verilenleri (True) ve Yasaklananları (False) ayır
                var grantedOverrides = userOverrides.Where(up => up.IsGranted).Select(up => up.Permission.Name);
                var deniedOverrides = userOverrides.Where(up => !up.IsGranted).Select(up => up.Permission.Name);

                // Adım D: Ana Mantık -> Rol yetkilerinden yasaklıları çıkar, üstüne özel izinleri ekle
                userPermissions = rolePermissions
                    .Except(deniedOverrides) 
                    .Union(grantedOverrides) 
                    .Distinct()
                    .ToList();

                // Adım E: Hesaplanan bu nihai listeyi 1 saatliğine Redis Cache'e yaz
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                };
                await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(userPermissions), cacheOptions);
            }

            // 4. Son Kontrol: İstek atılan yetki (Örn: "Catalog.Manage") bu listede var mı?
            if (userPermissions.Contains(requirement.Permission))
            {
                context.Succeed(requirement); // Kapıyı Aç!
            }
        }
    }
}