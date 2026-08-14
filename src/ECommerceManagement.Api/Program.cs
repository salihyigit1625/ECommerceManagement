using FluentValidation;
using FluentValidation.AspNetCore;
using ECommerceManagement.Application.Validations.Auth;
using System.Text;
using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Application.Mappings;
using ECommerceManagement.Application.Services;
using ECommerceManagement.Infrastructure.Security;
using ECommerceManagement.Infrastructure.Services;
using ECommerceManagement.Repository.Context;
using ECommerceManagement.Repository.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; 
using Serilog;
using System.Threading.RateLimiting;
using ECommerceManagement.Repository.Seed;
using Microsoft.AspNetCore.RateLimiting;
using SysmondAx.Integration.Handlers;
using SysmondAx.Integration.Models.Settings;
using SysmondAx.Integration.Services.Auth;
using SysmondAx.Integration.Services.Invoice;
using SysmondAx.Integration.Services.Order;
using SysmondAx.Integration.Services.Stock;
using SysmondAx.Integration.Services.Warehouse;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SysmondAxSettings>(builder.Configuration.GetSection("SysmondAxSettings"));

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// ==========================================
// 1. SERVİS KAYITLARI
// ==========================================
builder.Services.AddControllers();

builder.Services.AddAutoMapper(config => 
{
    config.AddProfile<MappingProfile>();
});

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CustomerRegisterDtoValidator>();

// ==========================================
// RATE LIMITING CONFIGURATION
// ==========================================
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // 1. Hassas Noktalar (Auth / Login / Register) İçin Katı Limit (Fixed Window)
    // 1 dakikada aynı IP'den en fazla 5 istek yapılabilir.
    options.AddFixedWindowLimiter("AuthPolicy", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0; // Kuyruğa alma, direkt reddet
    });

    // 2. Genel API Uç Noktaları İçin Esnek Limit (Sliding Window)
    // 1 dakikada en fazla 60 istek yapılabilir.
    options.AddSlidingWindowLimiter("GeneralPolicy", opt =>
    {
        opt.PermitLimit = 60;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.SegmentsPerWindow = 6;
        opt.QueueLimit = 0;
    });
});

// --- JWT & AUTHENTICATION SERVİSLERİ ---
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
    };
});

builder.Services.AddAuthorization();

// --- DİNAMİK YETKİLENDİRME (PERMISSION) KAYITLARI ---
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// --- SWAGGER & JWT KİLİT (AUTHORIZE) AYARI ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ECommerceManagement API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// DbContext Kaydı
builder.Services.AddDbContext<ECommerceDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Redis Kaydı
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
});

// Repository ve UnitOfWork Kayıtları
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();


// ==========================================
// SYSMOND ENTEGRASYON KAYITLARI
// ==========================================

// 0. Auth servisinin token'ı hafızada tutabilmesi için MemoryCache ekleniyor
builder.Services.AddMemoryCache(); // <-- EKLENDİ

// 1. Handler'ın ihtiyaç duyduğu Auth Servisinin Kaydı (Buna Handler TAKILMAZ!)
builder.Services.AddHttpClient<ISysmondAuthService, SysmondAuthService>(client => // <-- EKLENDİ
{
    client.BaseAddress = new Uri(builder.Configuration["SysmondAxSettings:BaseUrl"]!);
});

// 2. Önce Handler'ı transient olarak kaydediyoruz (Her HTTP isteğinde yeni bir token kontrolü yapabilmesi için)
builder.Services.AddTransient<SysmondAuthDelegatingHandler>();

// 3. Ardından HttpClient'ı kaydedip, içine bu Handler'ı takıyoruz
builder.Services.AddHttpClient<ISysmondWarehouseService, SysmondWarehouseService>(client =>
    {
        // Sysmond Base URL'ini API projesindeki appsettings.json'dan çekiyoruz
        client.BaseAddress = new Uri(builder.Configuration["SysmondAxSettings:BaseUrl"]!);
    })
    .AddHttpMessageHandler<SysmondAuthDelegatingHandler>();

builder.Services.AddHttpClient<ISysmondStockService, SysmondStockService>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["SysmondAxSettings:BaseUrl"]!);
    })
    .AddHttpMessageHandler<SysmondAuthDelegatingHandler>(); 

builder.Services.AddHttpClient<ISysmondOrderService, SysmondOrderService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["SysmondAxSettings:BaseUrl"]!);
})
    .AddHttpMessageHandler<SysmondAuthDelegatingHandler>();

builder.Services.AddHttpClient<ISysmondInvoiceService, SysmondInvoiceService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["SysmondAxSettings:BaseUrl"]!);
})
    .AddHttpMessageHandler<SysmondAuthDelegatingHandler>();


// Servis Kayıtları
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ISellerOrderService, SellerOrderService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<ICustomerOrderService, CustomerOrderService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAddressService, AddressService>();

//UYGULAMANIN İNŞASI (BUILD)
var app = builder.Build();


//serilog middleware
app.UseSerilogRequestLogging();

// HTTP REQUEST PIPELINE (Middleware'ler)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ECommerceManagement API v1");
        c.RoutePrefix = string.Empty; 
    });
}

app.UseHttpsRedirection();
//rate limiter
app.UseRateLimiter();

// Authentication mutlaka Authorization'dan ÖNCE yazılmalıdır!
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ==========================================
// DB SEEDING ON STARTUP
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
    await DbInitializer.SeedAsync(dbContext);
}

app.Run();