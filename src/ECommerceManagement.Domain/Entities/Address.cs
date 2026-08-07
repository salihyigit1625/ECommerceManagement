namespace ECommerceManagement.Domain.Entities;

public class Address : BaseEntity
{
    public int CustomerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public bool IsBilling { get; set; }
    public bool IsShipping { get; set; }

    public Customer Customer { get; set; } = null!;
}