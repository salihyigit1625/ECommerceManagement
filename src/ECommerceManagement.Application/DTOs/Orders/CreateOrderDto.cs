namespace ECommerceManagement.Application.DTOs.Orders;

public class CreateOrderDto
{
    public int CustomerId { get; set; }
    public int ShippingAddressId { get; set; }
    public int BillingAddressId { get; set; }
    
    public List<CreateOrderItemDto> Items { get; set; } = new();
}