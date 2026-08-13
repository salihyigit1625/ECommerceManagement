using System.Text.Json.Serialization;

namespace SysmondAx.Integration.Models.Dtos;

public class SysmondStockUpdateDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("companyId")]
    public Guid CompanyId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("brandName")]
    public string? BrandName { get; set; }

    [JsonPropertyName("modelName")]
    public string? ModelName { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; } = 10;

    [JsonPropertyName("vatPercent")]
    public decimal? VatPercent { get; set; }

    [JsonPropertyName("otvPercent")]
    public decimal? OtvPercent { get; set; }

    [JsonPropertyName("otvTaxCode")]
    public int OtvTaxCode { get; set; }

    [JsonPropertyName("gtip")]
    public string? Gtip { get; set; }

    [JsonPropertyName("actId")]
    public Guid? ActId { get; set; }

    [JsonPropertyName("measureUnitId")]
    public Guid? MeasureUnitId { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("stockTrackingEnabled")]
    public bool StockTrackingEnabled { get; set; }

    [JsonPropertyName("stockQuantityControlEnabled")]
    public bool StockQuantityControlEnabled { get; set; }
}