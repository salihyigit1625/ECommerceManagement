using System.Text.Json.Serialization;

namespace SysmondAx.Integration.Models.Dtos;

public class SysmondInvoiceDraftCreateDto
{
    [JsonPropertyName("actId")] public Guid ActId { get; set; }
    [JsonPropertyName("companyPeriodId")] public Guid CompanyPeriodId { get; set; }
    [JsonPropertyName("issueDate")] public string IssueDate { get; set; }
    [JsonPropertyName("scenario")] public int Scenario { get; set; } = 100; // 10 yerine 100 (Gerçek Payload)
    [JsonPropertyName("type")] public int Type { get; set; } = 10;
    [JsonPropertyName("currencyId")] public int CurrencyId { get; set; } = 949; // TRY 
    [JsonPropertyName("currencyExchangeRate")] public decimal CurrencyExchangeRate { get; set; } = 1;
    [JsonPropertyName("isDefaultAct")] public bool IsDefaultAct { get; set; } = false;
    [JsonPropertyName("eArchiveSendingType")] public int EArchiveSendingType { get; set; } = 10;

    // Gerçek İstekteki Sabit Adres ve Şablon ID'leri
    [JsonPropertyName("actAddressId")] public Guid? ActAddressId { get; set; }
    [JsonPropertyName("actCountryId")] public int? ActCountryId { get; set; } = 1;
    [JsonPropertyName("companyAddressId")] public Guid? CompanyAddressId { get; set; }
    [JsonPropertyName("companyContactAddressId")] public Guid? CompanyContactAddressId { get; set; }
    [JsonPropertyName("templateId")] public Guid? TemplateId { get; set; }

    [JsonPropertyName("orderDocRefs")] 
    public List<SysmondInvoiceOrderDocRefDto> OrderDocRefs { get; set; } = new();
}

public class SysmondInvoiceOrderDocRefDto
{
    [JsonPropertyName("type")] public int Type { get; set; } = 10;
    [JsonPropertyName("orderId")] public Guid OrderId { get; set; }
}

public class SysmondInvoiceItemCreateDto
{
    [JsonPropertyName("invoiceId")] public Guid InvoiceId { get; set; }
    [JsonPropertyName("stockId")] public Guid? StockId { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("quantity")] public decimal Quantity { get; set; }
    [JsonPropertyName("unitPrice")] public decimal UnitPrice { get; set; }
    [JsonPropertyName("vatPercent")] public decimal VatPercent { get; set; } = 0.2m;
    [JsonPropertyName("code")] public string? Code { get; set; }
    
    [JsonPropertyName("stockPriceId")] public Guid? StockPriceId { get; set; }
    
    [JsonPropertyName("measureUnitId")] public Guid? MeasureUnitId { get; set; }
    [JsonPropertyName("warehouseId")] public Guid? WarehouseId { get; set; }
    [JsonPropertyName("otvPercent")] public decimal OtvPercent { get; set; } = 10;
    [JsonPropertyName("otvTaxCode")] public int OtvTaxCode { get; set; } = 70;
    [JsonPropertyName("currencyExchangeRate")] public decimal CurrencyExchangeRate { get; set; } = 1;
}