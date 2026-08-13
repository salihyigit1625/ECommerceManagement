using System.Text.Json.Serialization;

namespace SysmondAx.Integration.Models.Dtos;

public class SysmondWarehouseStockDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("stockId")]
    public Guid StockId { get; set; }

    [JsonPropertyName("warehouseId")]
    public Guid WarehouseId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }
}