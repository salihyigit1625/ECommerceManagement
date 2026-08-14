using ECommerceManagement.Application.DTOs.Orders;

namespace ECommerceManagement.Application.Interfaces;

public interface ICustomerOrderService
{
    Task<IEnumerable<OrderDto>> GetMyOrdersAsync(int customerId);
    Task CreateOrderAsync(CreateOrderDto dto);
    Task CancelMyOrderAsync(int orderId, int customerId);
    Task SyncOrdersFromSysmondAsync();
}