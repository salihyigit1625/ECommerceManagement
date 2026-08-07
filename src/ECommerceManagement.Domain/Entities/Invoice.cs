using ECommerceManagement.Domain.Enums;
using ECommerceManagement.Domain.Entities;

namespace ECommerceManagement.Domain.Entities;

public class Invoice : BaseEntity
{
    public int OrderId { get; set; }
    public int SellerId { get; set; }
    public string CustomerName { get; set; } = string.Empty; // Anlık görüntü (Snapshot)
    public string? InvoiceNumber { get; set; }
    public decimal TotalAmount { get; set; }
    
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Waiting;
    public AxIntegrationStatus AxIntegrationStatus { get; set; } = AxIntegrationStatus.Pending;
    public string? AxIntegrationId { get; set; }

    public Order Order { get; set; } = null!;
    public Seller Seller { get; set; } = null!;

    public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
}

public class InvoiceItem : BaseEntity
{
    public int InvoiceId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }

    public Invoice Invoice { get; set; } = null!;
    public Product Product { get; set; } = null!;
}