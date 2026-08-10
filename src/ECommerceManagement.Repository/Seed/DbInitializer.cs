using ECommerceManagement.Domain.Entities;
using ECommerceManagement.Repository.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagement.Repository.Seed;

public static class DbInitializer
{
    public static async Task SeedAsync(ECommerceDbContext context)
    {
        var passwordHasher = new PasswordHasher<User>();
        var seedDate = new DateTime(2026, 8, 9, 0, 0, 0, DateTimeKind.Utc);

        // 1. SUPERADMIN
        if (!await context.Users.AnyAsync(u => u.Email == "superadmin@sistem.com"))
        {
            var superAdmin = new User
            {
                Username = "superadmin",
                Email = "superadmin@sistem.com",
                IsActive = true,
                CreatedAt = seedDate
            };
            superAdmin.PasswordHash = passwordHasher.HashPassword(superAdmin, "SuperAdmin123!");

            await context.Users.AddAsync(superAdmin);
            await context.SaveChangesAsync();

            await context.UserRoles.AddAsync(new UserRole { UserId = superAdmin.Id, RoleId = 1 });
            await context.SaveChangesAsync();
        }

        // 2. NORMAL ADMIN
        if (!await context.Users.AnyAsync(u => u.Email == "admin@sistem.com"))
        {
            var admin = new User
            {
                Username = "admin",
                Email = "admin@sistem.com",
                IsActive = true,
                CreatedAt = seedDate
            };
            admin.PasswordHash = passwordHasher.HashPassword(admin, "Admin123!");

            await context.Users.AddAsync(admin);
            await context.SaveChangesAsync();

            await context.UserRoles.AddAsync(new UserRole { UserId = admin.Id, RoleId = 2 });
            await context.SaveChangesAsync();
        }
    }
}