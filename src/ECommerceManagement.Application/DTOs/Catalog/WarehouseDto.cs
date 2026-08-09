namespace ECommerceManagement.Application.DTOs.Catalog;

public class WarehouseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
}