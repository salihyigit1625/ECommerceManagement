using System.Text.Json.Serialization;

namespace SysmondAx.Integration.Models.Dtos;

public class SysmondStockCreateDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    [JsonPropertyName("brandName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BrandName { get; set; }

    [JsonPropertyName("modelName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ModelName { get; set; }

    [JsonPropertyName("companyId")]
    public Guid CompanyId { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; } = 10; // 10 = Ticari Mal

    [JsonPropertyName("vatPercent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? VatPercent { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    // KRİTİK: Boş Guid gitmemesi için Guid? ve JsonIgnore yapıldı
    [JsonPropertyName("measureUnitId")]
    public Guid MeasureUnitId { get; set; } = Guid.Parse("ec2118e6-8154-7926-e418-3a2194605ce0");

    [JsonPropertyName("stockTrackingEnabled")]
    public bool StockTrackingEnabled { get; set; } = true;

    [JsonPropertyName("price")]
    public SysmondStockPriceDto? Price { get; set; }

    // Eğer açılış stoğu göndermiyorsak null girmesin, json'dan tamamen uçsun
    [JsonPropertyName("openingQuantity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<SysmondStockOpeningQuantityDto>? OpeningQuantity { get; set; }
}

public class SysmondStockPriceDto
{
    [JsonPropertyName("saleCurrencyId")]
    public int? SaleCurrencyId { get; set; } = 949;

    [JsonPropertyName("saleUnitPrice")]
    public decimal SaleUnitPrice { get; set; }

    [JsonPropertyName("purchaseCurrencyId")]
    public int? PurchaseCurrencyId { get; set; } = 949;

    [JsonPropertyName("purchaseUnitPrice")]
    public decimal PurchaseUnitPrice { get; set; }

    // KRİTİK: Boş Guid gitmemesi için Guid? ve JsonIgnore yapıldı
    [JsonPropertyName("measureUnitId")]
    public Guid MeasureUnitId { get; set; } = Guid.Parse("ec2118e6-8154-7926-e418-3a2194605ce0");
}

public class SysmondStockOpeningQuantityDto
{
    [JsonPropertyName("warehouseId")]
    public Guid WarehouseId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }
}