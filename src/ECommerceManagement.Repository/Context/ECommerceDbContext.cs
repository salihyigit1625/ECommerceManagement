using ECommerceManagement.Domain.Constants;
using ECommerceManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagement.Repository.Context;

public class ECommerceDbContext : DbContext
{
    public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : base(options) { }
    
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<UserPermission> UserPermissions { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Seller> Sellers { get; set; } = null!;
    public DbSet<Address> Addresses { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Warehouse> Warehouses { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<ProductImage> ProductImages { get; set; } = null!;
    public DbSet<ProductMovement> ProductMovements { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<InvoiceItem> InvoiceItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });
        modelBuilder.Entity<RolePermission>().HasKey(rp => new { rp.RoleId, rp.PermissionId });
        modelBuilder.Entity<UserPermission>().HasKey(up => new { up.UserId, up.PermissionId });

        modelBuilder.Entity<Customer>().HasOne(c => c.User).WithOne(u => u.Customer).HasForeignKey<Customer>(c => c.UserId);
        modelBuilder.Entity<Seller>().HasOne(s => s.User).WithOne(u => u.Seller).HasForeignKey<Seller>(s => s.UserId);
        modelBuilder.Entity<Invoice>().HasOne(i => i.Order).WithOne(o => o.Invoice).HasForeignKey<Invoice>(i => i.OrderId);

        modelBuilder.Entity<Order>().HasOne(o => o.Customer).WithMany(c => c.Orders).HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>().HasOne(o => o.Seller).WithMany(s => s.ReceivedOrders).HasForeignKey(o => o.SellerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>().HasOne(o => o.ShippingAddress).WithMany().HasForeignKey(o => o.ShippingAddressId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>().HasOne(o => o.BillingAddress).WithMany().HasForeignKey(o => o.BillingAddressId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Invoice>().HasOne(i => i.Seller).WithMany(s => s.IssuedInvoices).HasForeignKey(i => i.SellerId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Seller>().HasIndex(s => s.TaxNumber).IsUnique();
        modelBuilder.Entity<Invoice>().HasIndex(i => i.InvoiceNumber).IsUnique();

        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetProperties()).Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }
        
        // ==========================================
        // SEED DATA (SADECE SİSTEM ROLLERİ VE PERMISSION'LARI)
        // ==========================================
        var seedDate = new DateTime(2026, 8, 9, 0, 0, 0, DateTimeKind.Utc);

        // 1. ROLLER
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = AppRoles.SuperAdmin, CreatedAt = seedDate },
            new Role { Id = 2, Name = AppRoles.Admin, CreatedAt = seedDate },
            new Role { Id = 3, Name = AppRoles.Seller, CreatedAt = seedDate },
            new Role { Id = 4, Name = AppRoles.Customer, CreatedAt = seedDate }
        );

        // 2. YETKİ HAVUZU
        modelBuilder.Entity<Permission>().HasData(
            new Permission { Id = 1, Name = AppPermissions.ManageRoles, ModuleName = "Users", CreatedAt = seedDate },
            new Permission { Id = 2, Name = AppPermissions.ManagePermissions, ModuleName = "Users", CreatedAt = seedDate },
            new Permission { Id = 3, Name = AppPermissions.ManageCatalog, ModuleName = "Catalog", CreatedAt = seedDate },
            new Permission { Id = 4, Name = AppPermissions.ReadCatalog, ModuleName = "Catalog", CreatedAt = seedDate },
            new Permission { Id = 5, Name = AppPermissions.ManageWarehouses, ModuleName = "Warehouses", CreatedAt = seedDate },
            new Permission { Id = 6, Name = AppPermissions.ReadWarehouses, ModuleName = "Warehouses", CreatedAt = seedDate },
            new Permission { Id = 7, Name = AppPermissions.ManageProducts, ModuleName = "Products", CreatedAt = seedDate },
            new Permission { Id = 8, Name = AppPermissions.CreateOrders, ModuleName = "Orders", CreatedAt = seedDate },
            new Permission { Id = 9, Name = AppPermissions.ReadOrders, ModuleName = "Orders", CreatedAt = seedDate }
        );

        // 3. ROL & YETKİ EŞLEŞTİRMELERİ
        modelBuilder.Entity<RolePermission>().HasData(
            new RolePermission { RoleId = 2, PermissionId = 3 }, 
            new RolePermission { RoleId = 2, PermissionId = 4 }, 
            new RolePermission { RoleId = 2, PermissionId = 5 }, 
            new RolePermission { RoleId = 2, PermissionId = 6 },
            new RolePermission { RoleId = 3, PermissionId = 7 }, 
            new RolePermission { RoleId = 3, PermissionId = 9 }, 
            new RolePermission { RoleId = 3, PermissionId = 4 },
            new RolePermission { RoleId = 4, PermissionId = 8 }, 
            new RolePermission { RoleId = 4, PermissionId = 9 }, 
            new RolePermission { RoleId = 4, PermissionId = 4 }
        );
    }
}