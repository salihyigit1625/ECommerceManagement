using System.Text.Json.Serialization;

namespace SysmondAx.Integration.Models.Dtos;

public class SysmondOrderDraftCreateDto
{
    [JsonPropertyName("companyPeriodId")]
    public Guid CompanyPeriodId { get; set; } = Guid.Parse("e04ebee4-bdca-b9f1-ed45-3a22008d01a1");

    [JsonPropertyName("actId")]
    public Guid ActId { get; set; } = Guid.Parse("08006c31-a3bf-4f38-e29c-3a22008d01df");

    [JsonPropertyName("docNo")]
    public string? DocNo { get; set; } = null;

    [JsonPropertyName("orderDate")]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("direction")]
    public int Direction { get; set; } = 3; // (Satın alma Siparişi)

    [JsonPropertyName("currencyId")]
    public int CurrencyId { get; set; } = 949;

    [JsonPropertyName("currencyExchangeRate")]
    public decimal CurrencyExchangeRate { get; set; } = 1;
}

public class SysmondOrderItemCreateDto
{
    [JsonPropertyName("orderId")]
    public Guid OrderId { get; set; }

    [JsonPropertyName("stockId")]
    public Guid? StockId { get; set; }

    [JsonPropertyName("stockPriceId")]
    public Guid? StockPriceId { get; set; }

    [JsonPropertyName("measureUnitId")]
    public Guid MeasureUnitId { get; set; } = Guid.Parse("ec2118e6-8154-7926-e418-3a2194605ce0");

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("vatPercent")]
    public decimal VatPercent { get; set; } = 20; // %20 KDV

    [JsonPropertyName("isVatIncluded")]
    public bool IsVatIncluded { get; set; } = false;

    [JsonPropertyName("currencyExchangeRate")]
    public decimal CurrencyExchangeRate { get; set; } = 1;

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}