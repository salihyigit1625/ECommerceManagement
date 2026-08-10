using ECommerceManagement.Domain.Enums;
using ECommerceManagement.Domain.Entities;

namespace ECommerceManagement.Domain.Entities;

public class Order : BaseEntity
{
    public int CustomerId { get; set; }
    public int SellerId { get; set; }
    public int ShippingAddressId { get; set; }
    public int BillingAddressId { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime? UpdatedAt { get; set; }

    public Customer Customer { get; set; } = null!;
    public Seller Seller { get; set; } = null!;
    public Address ShippingAddress { get; set; } = null!;
    public Address BillingAddress { get; set; } = null!;
    public Invoice? Invoice { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}