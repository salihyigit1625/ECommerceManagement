using ECommerceManagement.Repository.Context;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. SERVİS KAYITLARI (Build edilmeden ÖNCE)
// ==========================================
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// DbContext Kaydı
builder.Services.AddDbContext<ECommerceDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

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