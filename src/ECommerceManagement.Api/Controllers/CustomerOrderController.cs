using ECommerceManagement.Domain.Constants;
using ECommerceManagement.Application.DTOs.Orders;
using ECommerceManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceManagement.Api.Controllers;

[Authorize(Roles = AppRoles.Customer)]
[ApiController]
[Route("api/[controller]")]
public class CustomerOrderController : ControllerBase
{
    private readonly ICustomerOrderService _customerOrderService;

    public CustomerOrderController(ICustomerOrderService customerOrderService)
    {
        _customerOrderService = customerOrderService;
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetMyOrders([FromQuery] int customerId)
    {
        var orders = await _customerOrderService.GetMyOrdersAsync(customerId);
        return Ok(orders);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        await _customerOrderService.CreateOrderAsync(dto);
        
        return Ok(new { Message = "Siparişler başarıyla oluşturuldu." });
    }

    [HttpPut("cancel")]
    public async Task<IActionResult> CancelOrder([FromBody] CancelOrderDto dto)
    {
        await _customerOrderService.CancelMyOrderAsync(dto.OrderId, dto.CustomerId);
        
        return Ok(new { Message = "Sipariş başarıyla iptal edildi." });
    }
}