namespace ECommerceManagement.Domain.Entities;

public class Category : BaseEntity
{
    public int? ParentCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;

    public Category? ParentCategory { get; set; }
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}