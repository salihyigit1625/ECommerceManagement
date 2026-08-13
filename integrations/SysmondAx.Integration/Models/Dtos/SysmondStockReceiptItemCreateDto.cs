using System.Text.Json.Serialization;

namespace SysmondAx.Integration.Models.Dtos;

public class SysmondStockReceiptItemCreateDto
{
    [JsonPropertyName("stockReceiptId")]
    public Guid StockReceiptId { get; set; } // Hangi fişin içine eklenecek?

    [JsonPropertyName("stockId")]
    public Guid StockId { get; set; } // Güncellenecek ürünün Sysmond ID'si

    [JsonPropertyName("warehouseId")]
    public Guid WarehouseId { get; set; } // Hangi depoya girecek/çıkacak?

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("stockPriceId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? StockPriceId { get; set; }

    [JsonPropertyName("currencyExchangeRate")]
    public decimal CurrencyExchangeRate { get; set; } = 1;
}