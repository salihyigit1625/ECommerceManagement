using SysmondAx.Integration.Models.Dtos;

namespace SysmondAx.Integration.Services.Order;

public interface ISysmondOrderService
{
    Task<Guid> CreateDraftOrderAsync(SysmondOrderDraftCreateDto dto);
    Task AddOrderItemAsync(SysmondOrderItemCreateDto dto);
    Task DeleteOrderAsync(Guid orderId);
    Task UpdateOrderStatusAsync(SysmondOrderStatusUpdateDto dto);
    Task<List<SysmondOrderDto>> GetOrderStatusesByIdsAsync(List<Guid> orderIds);
    Task<List<SysmondOrderDto>> GetAllOrdersAsync();
    Task<List<SysmondOrderItemDto>> GetOrderItemsAsync(Guid orderId);
    
}