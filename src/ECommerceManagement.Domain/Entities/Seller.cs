
namespace ECommerceManagement.Domain.Entities;

public class Seller : BaseEntity
{
    public int UserId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;

    // Navigation Properties
    public User User { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Order> ReceivedOrders { get; set; } = new List<Order>();
    public ICollection<Invoice> IssuedInvoices { get; set; } = new List<Invoice>();
}