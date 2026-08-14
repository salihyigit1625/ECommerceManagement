using System.Net.Http.Json;
using SysmondAx.Integration.Models.Dtos;

namespace SysmondAx.Integration.Services.Invoice;

public class SysmondInvoiceService : ISysmondInvoiceService
{
    private readonly HttpClient _httpClient;

    public SysmondInvoiceService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Guid> CreateDraftInvoiceAsync(SysmondInvoiceDraftCreateDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/app/outgoing-invoice/draft", dto);
        
        string content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Sysmond Fatura Taslağı oluşturulamadı: {content}");

        var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(content);
        string? invoiceIdStr = jsonNode?["id"]?.ToString() ?? jsonNode?["data"]?["id"]?.ToString();
        
        if (Guid.TryParse(invoiceIdStr, out Guid invoiceId))
            return invoiceId;

        throw new Exception("Sysmond geçerli bir Fatura ID'si dönmedi.");
    }

    public async Task AddInvoiceItemAsync(SysmondInvoiceItemCreateDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/app/outgoing-invoice/item", dto);
        
        if (!response.IsSuccessStatusCode)
        {
            string content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Sysmond faturaya kalem eklenemedi: {content}");
        }
    }
}