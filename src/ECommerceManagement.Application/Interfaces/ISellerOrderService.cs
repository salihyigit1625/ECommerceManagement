using ECommerceManagement.Application.DTOs.Invoices;
using ECommerceManagement.Application.DTOs.Orders;

namespace ECommerceManagement.Application.Interfaces;

public interface ISellerOrderService
{
    Task<IEnumerable<OrderDto>> GetPendingOrdersAsync(int sellerId);
    Task<InvoiceDto> CreateInvoiceDraftAsync(int orderId, int sellerId);
    Task ConfirmInvoiceAndOrderAsync(int invoiceId, int sellerId);
    Task ShipOrderAsync(int orderId, int sellerId);
    Task CancelOrderAsync(int orderId, int sellerId);
}