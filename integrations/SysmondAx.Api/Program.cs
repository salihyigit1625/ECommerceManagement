using SysmondAx.Integration.Handlers;
using SysmondAx.Integration.Models.Settings;
using SysmondAx.Integration.Services.Auth;
using SysmondAx.Integration.Services.Stock;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SysmondAxSettings>(builder.Configuration.GetSection("SysmondAxSettings"));

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<ISysmondAuthService, SysmondAuthService>();
builder.Services.AddTransient<SysmondAuthDelegatingHandler>();

builder.Services.AddHttpClient<ISysmondStockService, SysmondStockService>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["SysmondAxSettings:BaseUrl"]!);
    })
    .AddHttpMessageHandler<SysmondAuthDelegatingHandler>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

