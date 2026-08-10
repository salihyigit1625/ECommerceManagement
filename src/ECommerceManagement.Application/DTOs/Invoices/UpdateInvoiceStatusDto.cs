using ECommerceManagement.Domain.Enums;

namespace ECommerceManagement.Application.DTOs.Invoices;

public class UpdateInvoiceStatusDto
{
    public int InvoiceId { get; set; }
    public InvoiceStatus NewStatus { get; set; }
}