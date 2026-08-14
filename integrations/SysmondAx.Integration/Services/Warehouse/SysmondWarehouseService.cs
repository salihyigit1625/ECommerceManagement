using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using SysmondAx.Integration.Models;
using SysmondAx.Integration.Models.Dtos;
using SysmondAx.Integration.Models.Requests;

namespace SysmondAx.Integration.Services.Warehouse;

public class SysmondWarehouseService : ISysmondWarehouseService
{
    private readonly HttpClient _httpClient;
    
    private readonly Guid _sysmondCompanyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");

    public SysmondWarehouseService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> CreateWarehouseAsync(SysmondWarehouseRequest request)
    {
        string code = string.IsNullOrWhiteSpace(request.WarehouseCode) 
            ? request.Name.Replace(" ", "-").ToUpperInvariant() 
            : request.WarehouseCode;

        var sysmondDto = new SysmondWarehouseCreateDto
        {
            CompanyId = _sysmondCompanyId,
            Name = request.Name,
            WarehouseCode = code
        };

        var response = await _httpClient.PostAsJsonAsync("/api/app/warehouse", sysmondDto);

        if (response.IsSuccessStatusCode)
        {
            string jsonString = await response.Content.ReadAsStringAsync();
    
            var jsonNode = JsonNode.Parse(jsonString);
            return jsonNode?["data"]?["id"]?.ToString() ?? string.Empty;
        }

        string errorContent = await response.Content.ReadAsStringAsync();
        throw new Exception($"Sysmond Depo oluşturulamadı. Hata: {errorContent}");
    }
    
    public async Task<List<SysmondWarehouseDto>> GetWarehousesAsync()
    {
        Guid companyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");

        var response = await _httpClient.GetAsync($"/api/app/warehouse?companyId={companyId}");

        if (response.IsSuccessStatusCode)
        {
            string jsonString = await response.Content.ReadAsStringAsync();
            var jsonNode = JsonNode.Parse(jsonString);

            var itemsNode = jsonNode?["items"] ?? jsonNode?["data"];
        
            if (itemsNode != null)
            {
                var warehouses = itemsNode.Deserialize<List<SysmondWarehouseDto>>();
                return warehouses ?? new List<SysmondWarehouseDto>();
            }
        }

        string errorContent = await response.Content.ReadAsStringAsync();
        throw new Exception($"Sysmond depoları getirilemedi. Hata: {errorContent}");
    }
    
    public async Task<List<SysmondWarehouseStockDto>> GetWarehouseStocksAsync()
    {
        var response = await _httpClient.GetAsync($"/api/app/warehouse-stock?CompanyId={_sysmondCompanyId}");

        if (response.IsSuccessStatusCode)
        {
            string jsonString = await response.Content.ReadAsStringAsync();
            var jsonNode = JsonNode.Parse(jsonString);

            var itemsNode = jsonNode?["items"] ?? jsonNode?["data"];
        
            if (itemsNode != null)
            {
                return itemsNode.Deserialize<List<SysmondWarehouseStockDto>>() ?? new List<SysmondWarehouseStockDto>();
            }
        }

        string errorContent = await response.Content.ReadAsStringAsync();
        throw new Exception($"Sysmond depo-stok ilişkileri getirilemedi. Hata: {errorContent}");
    }
}