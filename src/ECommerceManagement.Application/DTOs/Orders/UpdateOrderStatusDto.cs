using ECommerceManagement.Domain.Enums;

namespace ECommerceManagement.Application.DTOs.Orders;

public class UpdateOrderStatusDto
{
    public int OrderId { get; set; }
    public OrderStatus NewStatus { get; set; }
}