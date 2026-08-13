namespace SysmondAx.Integration.Models.Requests;

public class SysmondStockRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Code { get; set; }
    public decimal SalePrice { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal? VatPercent { get; set; }
    
    public Guid? WarehouseSysmondId { get; set; }
    public decimal? InitialQuantity { get; set; }
}