using System.Text.Json.Serialization;

namespace SysmondAx.Integration.Models.Dtos;

public class SysmondStockReceiptCreateDto
{
    [JsonPropertyName("docNo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DocNo { get; set; }

    [JsonPropertyName("companyPeriodId")]
    public Guid CompanyPeriodId { get; set; } = Guid.Parse("e04ebee4-bdca-b9f1-ed45-3a22008d01a1");
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; } = 10; // Sysmond Mal Giriş Tipi

    [JsonPropertyName("transactionDate")]
    public DateTime? TransactionDate { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("warehouseId")]
    public Guid WarehouseId { get; set; }

    [JsonPropertyName("targetWarehouseId")]
    public Guid? TargetWarehouseId { get; set; }
}
