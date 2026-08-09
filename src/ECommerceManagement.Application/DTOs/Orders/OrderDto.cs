using ECommerceManagement.Domain.Enums;

namespace ECommerceManagement.Application.DTOs.Orders;

public class OrderDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerFullName { get; set; } = string.Empty;
    public int SellerId { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public List<OrderItemDto> Items { get; set; } = new();
}