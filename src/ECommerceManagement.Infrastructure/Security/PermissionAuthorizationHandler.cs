using System.Security.Claims;
using System.Text.Json;
using ECommerceManagement.Domain.Constants;
using ECommerceManagement.Repository.Context;
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
            // 1. Kullanıcı doğrulanmamışsa direkt çık (401)
            if (context.User.Identity == null || !context.User.Identity.IsAuthenticated)
                return;

            // 2. SÜPER ADMİN KONTROLÜ
            var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value 
                        ?? context.User.FindFirst("role")?.Value;

            if (userRole == AppRoles.SuperAdmin)
            {
                context.Succeed(requirement);
                return;
            }

            // Token içinden Kullanıcı ID'sini al
            var userIdStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                         ?? context.User.FindFirst("nameid")?.Value 
                         ?? context.User.FindFirst("sub")?.Value;

            if (!int.TryParse(userIdStr, out int userId))
                return;

            using var scope = _scopeFactory.CreateScope();
            var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

            string cacheKey = $"permissions_user_{userId}";
            List<string> userPermissions = new();

            var cachedPermissions = await cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedPermissions))
            {
                userPermissions = JsonSerializer.Deserialize<List<string>>(cachedPermissions) ?? new List<string>();
            }
            else
            {
                var rolePermissions = await dbContext.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => rp.Permission.Name)
                    .ToListAsync();

                var userOverrides = await dbContext.UserPermissions
                    .Include(up => up.Permission)
                    .Where(up => up.UserId == userId)
                    .ToListAsync();

                var grantedOverrides = userOverrides.Where(up => up.IsGranted).Select(up => up.Permission.Name);
                var deniedOverrides = userOverrides.Where(up => !up.IsGranted).Select(up => up.Permission.Name);

                userPermissions = rolePermissions
                    .Except(deniedOverrides) 
                    .Union(grantedOverrides) 
                    .Distinct()
                    .ToList();

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                };
                await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(userPermissions), cacheOptions);
            }

            if (userPermissions.Contains(requirement.Permission))
            {
                context.Succeed(requirement);
            }
        }
    }
}