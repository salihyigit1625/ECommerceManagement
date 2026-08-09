using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Application.Services;
using ECommerceManagement.Repository.Context;
using ECommerceManagement.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. SERVİS KAYITLARI
// ==========================================
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// DbContext Kaydı
builder.Services.AddDbContext<ECommerceDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Repository ve UnitOfWork Kayıtları
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
// Servis Kayıtları
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ISellerOrderService, SellerOrderService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<ICustomerOrderService, CustomerOrderService>();
builder.Services.AddScoped<IAdminService, AdminService>();

// ==========================================
// 2. UYGULAMANIN İNŞASI (BUILD)
// ==========================================
var app = builder.Build();

// ==========================================
// 3. HTTP REQUEST PIPELINE (Middleware'ler)
// ==========================================
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization(); // Yetkilendirme middleware'i

app.MapControllers(); // Controller'ları ayağa kaldırmak için gerekli

app.Run();