using SysmondAx.Integration.Handlers;
using SysmondAx.Integration.Models.Settings;
using SysmondAx.Integration.Services.Auth;
using SysmondAx.Integration.Services.Warehouse;

var builder = WebApplication.CreateBuilder(args);

// 1. AppSettings Ayarlarının Yüklenmesi
builder.Services.Configure<SysmondAxSettings>(builder.Configuration.GetSection("SysmondAxSettings"));

// BaseUrl değerini güvenli bir şekilde alıyoruz
var baseUrl = builder.Configuration["SysmondAxSettings:BaseUrl"];
if (string.IsNullOrEmpty(baseUrl))
{
    throw new InvalidOperationException("'SysmondAxSettings:BaseUrl' değeri appsettings.json dosyasında bulunamadı!");
}

builder.Services.AddMemoryCache();

// 2. Controller Desteğinin Eklenmesi (app.MapControllers için şarttır)
builder.Services.AddControllers();

// 3. Auth Servisinin Kaydı (BaseAddress Eklendi - Handler TAKILMADI!)
builder.Services.AddHttpClient<ISysmondAuthService, SysmondAuthService>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
});

// 4. Token Handler Kaydı (Transient olarak)
builder.Services.AddTransient<SysmondAuthDelegatingHandler>();


// 6. Warehouse Servisinin Kaydı
builder.Services.AddHttpClient<ISysmondWarehouseService, SysmondWarehouseService>(client =>
    {
        client.BaseAddress = new Uri(baseUrl);
    })
    .AddHttpMessageHandler<SysmondAuthDelegatingHandler>();

// OpenAPI / Swagger Desteği
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// MapControllers'ın çalışması için AddControllers() yukarıda eklenmiştir
app.MapControllers();

app.Run();