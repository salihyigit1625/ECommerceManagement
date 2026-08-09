using ECommerceManagement.Application.DTOs.Orders;
using ECommerceManagement.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerOrderController : ControllerBase
{
    private readonly ICustomerOrderService _customerOrderService;

    public CustomerOrderController(ICustomerOrderService customerOrderService)
    {
        _customerOrderService = customerOrderService;
    }

    [HttpGet("{customerId}/orders")]
    public async Task<IActionResult> GetMyOrders(int customerId)
    {
        var orders = await _customerOrderService.GetMyOrdersAsync(customerId);
        return Ok(orders);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        // Sepetteki ürünler satıcılara göre bölünür, ayrı siparişler oluşur ve stoklar düşer
        await _customerOrderService.CreateOrderAsync(dto);
        
        return Ok(new { Message = "Siparişler başarıyla oluşturuldu." });
    }

    [HttpPut("{customerId}/orders/{orderId}/cancel")]
    public async Task<IActionResult> CancelOrder(int customerId, int orderId)
    {
        // Sadece "Pending" durumundaki siparişler iptal edilebilir
        await _customerOrderService.CancelMyOrderAsync(orderId, customerId);
        
        return Ok(new { Message = "Sipariş başarıyla iptal edildi." });
    }
}