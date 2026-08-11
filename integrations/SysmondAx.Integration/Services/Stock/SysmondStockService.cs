using System.Net.Http.Json;
using SysmondAx.Integration.Services.Stock;

namespace SysmondAx.Integration.Services.Stock;

public class SysmondStockService : ISysmondStockService
{
    private readonly HttpClient _httpClient;

    // Burada Auth servisini inject etmiyoruz! Çünkü DelegatingHandler arka planda token'ı otomatik ekleyecek.
    public SysmondStockService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> CreateStockCardAsync(object productDto)
    {
        // 1. Kendi ürün modelini Sysmond'un beklediği DTO'ya dönüştür (Mapping)
        // Not: Swagger'dan veya Postman'den StockCreateDto yapısını alıp buraya model olarak eklemelisin.
        var sysmondStockDto = new 
        {
            // Örnek alanlar (Swagger'daki asıl alanlarla eşleştireceksin)
            // type = 10,
            // code = "URUN-001",
            // name = "Örnek E-Ticaret Ürünü",
            // vatRate = 20
        };

        // 2. Sysmond endpoint'ine POST isteği at
        // BaseUrl Program.cs'den otomatik gelecek, sadece endpoint path'ini yazıyoruz
        var response = await _httpClient.PostAsJsonAsync("/api/app/stock", sysmondStockDto);

        if (response.IsSuccessStatusCode)
        {
            // Başarılıysa Sysmond'un döndüğü ID'yi alıyoruz
            var result = await response.Content.ReadFromJsonAsync<dynamic>(); 
            return result?.data?.id?.ToString() ?? string.Empty;
        }

        // Hata durumunda loglama veya exception fırlatma
        string errorContent = await response.Content.ReadAsStringAsync();
        throw new Exception($"Sysmond Stok Kartı oluşturulamadı. Hata: {errorContent}");
    }
}