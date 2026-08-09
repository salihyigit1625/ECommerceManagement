namespace ECommerceManagement.Application.DTOs.Invoices;

public class CreateInvoiceDto
{
    public int OrderId { get; set; }
    public int SellerId { get; set; }
    // Fatura kalemleri (Items), veritabanındaki sipariş kalemlerinden (OrderItems) sistem tarafından otomatik üretilecek.
}