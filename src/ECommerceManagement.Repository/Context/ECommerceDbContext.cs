using ECommerceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagement.Repository.Context;

public class ECommerceDbContext : DbContext
{
    public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : base(options)
    {
    }

    // Auth & Authorization
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<UserPermission> UserPermissions { get; set; } = null!;

    // Profiles & Addresses
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Seller> Sellers { get; set; } = null!;
    public DbSet<Address> Addresses { get; set; } = null!;

    // Catalog & Warehouse
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Warehouse> Warehouses { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<ProductImage> ProductImages { get; set; } = null!;
    public DbSet<ProductMovement> ProductMovements { get; set; } = null!;

    // Order & Invoicing
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<InvoiceItem> InvoiceItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. COMPOSITE KEYS (Birleşik Anahtarlar - Ara Tablolar İçin)
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        modelBuilder.Entity<UserPermission>()
            .HasKey(up => new { up.UserId, up.PermissionId });

        // 2. ONE-TO-ONE RELATIONSHIPS (1'e 1 İlişkiler - Ters FK)
        modelBuilder.Entity<Customer>()
            .HasOne(c => c.User)
            .WithOne(u => u.Customer)
            .HasForeignKey<Customer>(c => c.UserId);

        modelBuilder.Entity<Seller>()
            .HasOne(s => s.User)
            .WithOne(u => u.Seller)
            .HasForeignKey<Seller>(s => s.UserId);

        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.Order)
            .WithOne(o => o.Invoice)
            .HasForeignKey<Invoice>(i => i.OrderId);

        // 3. RESTRICT DELETE BEHAVIORS (Cascade Silmeleri Engelleme)
        // Özellikle sipariş tablolarında veri silindiğinde birbirini tetikleyerek patlamasını engelliyoruz.
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Seller)
            .WithMany(s => s.ReceivedOrders)
            .HasForeignKey(o => o.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.ShippingAddress)
            .WithMany()
            .HasForeignKey(o => o.ShippingAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.BillingAddress)
            .WithMany()
            .HasForeignKey(o => o.BillingAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.Seller)
            .WithMany(s => s.IssuedInvoices)
            .HasForeignKey(i => i.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        // 4. UNIQUE CONSTRAINTS (Benzersiz Alanlar)
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Seller>()
            .HasIndex(s => s.TaxNumber)
            .IsUnique();

        modelBuilder.Entity<Invoice>()
            .HasIndex(i => i.InvoiceNumber)
            .IsUnique();

        // 5. PRECISION SETTINGS (Decimal Alanlar İçin Hassasiyet Ayarı)
        // Kurumsal bir uygulamada para değerlerinin veritabanında kaç hane tutulacağını mutlaka belirtmeliyiz.
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }
    }
}