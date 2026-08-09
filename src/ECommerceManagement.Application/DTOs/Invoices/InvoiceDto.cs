using ECommerceManagement.Domain.Enums;

namespace ECommerceManagement.Application.DTOs.Invoices;

public class InvoiceDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string? InvoiceNumber { get; set; }
    public string CustomerName { get; set; } = string.Empty; // Snapshot
    public decimal TotalAmount { get; set; }
    public InvoiceStatus Status { get; set; }
    public AxIntegrationStatus AxIntegrationStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public List<InvoiceItemDto> Items { get; set; } = new();
}