using ECommerceManagement.Domain.Constants;
using ECommerceManagement.Application.DTOs.Catalog;
using ECommerceManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceManagement.Api.Controllers;

[Authorize(Roles = AppRoles.Seller)]
[ApiController]
[Route("api/[controller]")]
public class SellerController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ISellerOrderService _sellerOrderService;

    public SellerController(IProductService productService, ISellerOrderService sellerOrderService)
    {
        _productService = productService;
        _sellerOrderService = sellerOrderService;
    }
    
    [HttpGet("products")]
    public async Task<IActionResult> GetMyProducts([FromQuery] int sellerId)
    {
        var products = await _productService.GetProductsBySellerIdAsync(sellerId);
        return Ok(products);
    }

    [HttpPost("products")]
    public async Task<IActionResult> AddProduct([FromBody] CreateProductDto dto)
    {
        await _productService.AddAsync(dto);
        return Ok(new { Message = "Ürün başarıyla eklendi." });
    }

    [HttpPut("products")]
    public async Task<IActionResult> UpdateProduct([FromBody] UpdateProductDto dto)
    {
        await _productService.UpdateProductAsync(dto.Id,dto);
        return Ok(new { Message = "Ürün başarıyla güncellendi." });
    }

    [HttpDelete("products")]
    public async Task<IActionResult> DeleteProduct([FromQuery]int id)
    {
        await _productService.DeleteProductAsync(id);
        return Ok(new { Message = "Ürün başarıyla silindi (pasife çekildi)." });
    }

    [HttpGet("/orders/pending")]
    public async Task<IActionResult> GetPendingOrders([FromQuery]int sellerId)
    {
        var orders = await _sellerOrderService.GetPendingOrdersAsync(sellerId);
        return Ok(orders);
    }

    // TEK ADIMDA FATURA VE ONAY
    [HttpPost("/orders/invoice")]
    public async Task<IActionResult> CreateAndConfirmInvoice([FromQuery]int sellerId, [FromQuery] int orderId)
    {
        var invoice = await _sellerOrderService.CreateAndConfirmInvoiceAsync(orderId, sellerId);
        return Ok(invoice);
    }

    // SYSMOND'A DA 30 STATÜSÜNÜ İLETEN GÜNCEL SHIP METODU
    [HttpPut("/orders/ship")]
    public async Task<IActionResult> ShipOrder([FromQuery]int sellerId, [FromQuery]int orderId)
    {
        await _sellerOrderService.ShipOrderAsync(orderId, sellerId);
        return Ok(new { Message = "Sipariş kargoya verildi. Durum: 'Shipped'." });
    }
    
    [HttpGet("/orders/all")]
    public async Task<IActionResult> GetAllOrders([FromQuery]int sellerId)
    {
        var orders = await _sellerOrderService.GetAllOrdersBySellerIdAsync(sellerId);
        return Ok(orders);
    }

    [HttpGet("/invoices")]
    public async Task<IActionResult> GetInvoices([FromQuery]int sellerId)
    {
        var invoices = await _sellerOrderService.GetInvoicesBySellerIdAsync(sellerId);
        return Ok(invoices);
    }
}