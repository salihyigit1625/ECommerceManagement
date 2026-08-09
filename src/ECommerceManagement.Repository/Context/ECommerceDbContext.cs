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
        
        // ==========================================
        // SEED DATA (Başlangıç Test Verileri)
        // ==========================================
    
        // Not: HasData içinde DateTime.UtcNow kullanmak yerine sabit bir tarih vermek daha sağlıklıdır, 
        // aksi halde EF Core her migration'da tarih değiştiği için kayıtları güncellemeye çalışır.
        var seedDate = new DateTime(2026, 8, 9, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Username = "satici_ahmet", Email = "ahmet@test.com", PasswordHash = "dummy_hash", IsActive = true, CreatedAt = seedDate },
            new User { Id = 2, Username = "musteri_mehmet", Email = "mehmet@test.com", PasswordHash = "dummy_hash", IsActive = true, CreatedAt = seedDate }
        );

        modelBuilder.Entity<Seller>().HasData(
            new Seller { Id = 1, UserId = 1, CompanyName = "Ahmet Teknoloji", TaxNumber = "123456789", ContactEmail = "iletisim@ahmet.com", CreatedAt = seedDate }
        );

        modelBuilder.Entity<Customer>().HasData(
            new Customer { Id = 1, UserId = 2, FirstName = "Mehmet", LastName = "Yılmaz", Phone = "5551234567", CreatedAt = seedDate }
        );

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Bilgisayar Bileşenleri", CreatedAt = seedDate }
        );

        // Warehouse'da SellerId YOK, düz platform deposu
        modelBuilder.Entity<Warehouse>().HasData(
            new Warehouse { Id = 1, Name = "Gebze Ana Depo", Location = "Kocaeli", IsActive = true, CreatedAt = seedDate }
        );

        modelBuilder.Entity<Address>().HasData(
            new Address { Id = 1, CustomerId = 1, Title = "Ev Adresi", City = "Bursa", District = "Nilüfer", FullAddress = "Ata Bulvarı No:1", IsBilling = true, IsShipping = true, CreatedAt = seedDate }
        );
        
        
    }
}