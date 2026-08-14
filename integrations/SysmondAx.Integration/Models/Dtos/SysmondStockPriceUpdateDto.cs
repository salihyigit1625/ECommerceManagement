using System.Text.Json.Serialization;

namespace SysmondAx.Integration.Models.Dtos;

public class SysmondStockPriceUpdateDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("stockId")]
    public Guid StockId { get; set; }

    [JsonPropertyName("currencyId")]
    public int? CurrencyId { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("stockPriceTypeId")]
    public Guid? StockPriceTypeId { get; set; }

    [JsonPropertyName("measureUnitId")]
    public Guid? MeasureUnitId { get; set; }

    [JsonPropertyName("isDefaultMeasureUnitPrice")]
    public bool IsDefaultMeasureUnitPrice { get; set; } = true;
}