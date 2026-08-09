namespace ECommerceManagement.Application.DTOs.Catalog;

public class CreateProductDto
{
    public int SellerId { get; set; }
    public int CategoryId { get; set; }
    public int WarehouseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}