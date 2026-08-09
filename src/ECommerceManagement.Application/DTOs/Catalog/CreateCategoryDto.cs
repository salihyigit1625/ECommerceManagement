namespace ECommerceManagement.Application.DTOs.Catalog;

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
}