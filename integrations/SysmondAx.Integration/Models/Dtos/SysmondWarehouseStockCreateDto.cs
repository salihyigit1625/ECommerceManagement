using System.Text.Json.Serialization;

namespace SysmondAx.Integration.Models.Dtos;

public class SysmondWarehouseStockCreateDto
{
    [JsonPropertyName("warehouseId")]
    public Guid WarehouseId { get; set; }

    [JsonPropertyName("stockId")]
    public Guid StockId { get; set; }

    [JsonPropertyName("criticalStockLevel")]
    public double? CriticalStockLevel { get; set; }
}