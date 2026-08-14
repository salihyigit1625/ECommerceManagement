using System.Net.Http.Json;
using SysmondAx.Integration.Models.Dtos;

namespace SysmondAx.Integration.Services.Order;

public class SysmondOrderService : ISysmondOrderService
{
    private readonly HttpClient _httpClient;

    public SysmondOrderService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Guid> CreateDraftOrderAsync(SysmondOrderDraftCreateDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/app/order/draft", dto);
        
        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Sysmond taslak sipariş oluşturulamadı. Hata: {error}");
        }

        string jsonString = await response.Content.ReadAsStringAsync();
        var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(jsonString);
        
        string? orderIdStr = jsonNode?["id"]?.ToString() ?? jsonNode?["data"]?["id"]?.ToString();
        if (string.IsNullOrEmpty(orderIdStr) || !Guid.TryParse(orderIdStr, out Guid orderId))
        {
            throw new Exception("Sysmond'dan geçerli bir Order ID dönmedi.");
        }

        return orderId;
    }

    public async Task AddOrderItemAsync(SysmondOrderItemCreateDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/app/order/item", dto);
        
        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Sysmond sipariş kalemi eklenemedi. Hata: {error}");
        }
    }

    public async Task DeleteOrderAsync(Guid orderId)
    {
        var response = await _httpClient.DeleteAsync($"/api/app/order/{orderId}");
        
        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Sysmond taslak siparişi silinemedi. Hata: {error}");
        }
    }
    
    public async Task UpdateOrderStatusAsync(SysmondOrderStatusUpdateDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync("/api/app/order/status", dto);
    
        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Sysmond sipariş durumu güncellenemedi (İptal işlemi başarısız). StatusCode: {response.StatusCode} | Hata: {error}");
        }
    }
    
    public async Task<List<SysmondOrderDto>> GetOrderStatusesByIdsAsync(List<Guid> orderIds)
    {
        if (orderIds == null || !orderIds.Any()) 
            return new List<SysmondOrderDto>();

        string companyId = "f9e4c15a-307a-d6e5-495a-3a22008d01a1"; 
        var url = $"/api/app/order-query/orders?CompanyId={companyId}&Direction=3&MaxResultCount=100";

        foreach (var id in orderIds)
        {
            url += $"&Ids={id}";
        }

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            return new List<SysmondOrderDto>();
        }

        var result = await response.Content.ReadFromJsonAsync<SysmondOrderQueryResponseDto>();
        return result?.GetOrders() ?? new List<SysmondOrderDto>();
    }
    
    public async Task<List<SysmondOrderDto>> GetAllOrdersAsync()
    {
        string companyId = "f9e4c15a-307a-d6e5-495a-3a22008d01a1";
    
        var url = $"/api/app/order-query/orders?CompanyId={companyId}&Direction=3&MaxResultCount=1000";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode) return new List<SysmondOrderDto>();

        var result = await response.Content.ReadFromJsonAsync<SysmondOrderQueryResponseDto>();
        return result?.GetOrders() ?? new List<SysmondOrderDto>();
    }

    public async Task<List<SysmondOrderItemDto>> GetOrderItemsAsync(Guid orderId)
    {
        var response = await _httpClient.GetAsync($"/api/app/order-query/{orderId}/order-items");
        if (!response.IsSuccessStatusCode) return new List<SysmondOrderItemDto>();

        var result = await response.Content.ReadFromJsonAsync<SysmondOrderItemResponseDto>();
        return result?.GetItems() ?? new List<SysmondOrderItemDto>();
    }
}