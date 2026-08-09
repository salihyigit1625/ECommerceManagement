namespace ECommerceManagement.Application.DTOs.Orders;

public class CreateOrderDto
{
    public int CustomerId { get; set; }
    public int ShippingAddressId { get; set; }
    public int BillingAddressId { get; set; }
    
    // Müşteri sepetteki ürünleri yollar. Sistem arka planda bunları satıcılara göre bölecek.
    public List<CreateOrderItemDto> Items { get; set; } = new();
}