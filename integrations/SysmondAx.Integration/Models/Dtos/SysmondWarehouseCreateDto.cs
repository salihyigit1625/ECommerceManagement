using System.Text.Json.Serialization;

namespace SysmondAx.Integration.Models.Dtos;

public class SysmondWarehouseCreateDto
{
    [JsonPropertyName("companyId")]
    public Guid CompanyId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("warehouseCode")]
    public string? WarehouseCode { get; set; }
}