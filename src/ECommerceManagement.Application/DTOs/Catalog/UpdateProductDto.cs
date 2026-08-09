namespace ECommerceManagement.Application.DTOs.Catalog;

public class UpdateProductDto
{
    public int Id { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public bool IsActive { get; set; }
}