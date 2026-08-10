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

    // Servislerimizi (İş Kurallarımızı) içeri alıyoruz (Dependency Injection)
    public SellerController(IProductService productService, ISellerOrderService sellerOrderService)
    {
        _productService = productService;
        _sellerOrderService = sellerOrderService;
    }

    // ==========================================
    // 1. ÜRÜN VE STOK YÖNETİMİ
    // ==========================================

    [HttpGet("products")]
    public async Task<IActionResult> GetMyProducts([FromQuery] int sellerId)
    {
        var products = await _productService.GetProductsBySellerIdAsync(sellerId);
        return Ok(products); // HTTP 200 döner
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
        await _productService.UpdateAsync(dto);
        return Ok(new { Message = "Ürün başarıyla güncellendi." });
    }

    [HttpDelete("products")]
    public async Task<IActionResult> DeleteProduct([FromQuery]int id)
    {
        await _productService.DeleteAsync(id);
        return Ok(new { Message = "Ürün başarıyla silindi (pasife çekildi)." });
    }

    // ==========================================
    // 2. SİPARİŞ VE FATURA YÖNETİMİ
    // ==========================================

    [HttpGet("/orders/pending")]
    public async Task<IActionResult> GetPendingOrders([FromQuery]int sellerId)
    {
        var orders = await _sellerOrderService.GetPendingOrdersAsync(sellerId);
        return Ok(orders);
    }

    [HttpPost("/orders/invoice")]
    public async Task<IActionResult> CreateInvoiceDraft([FromQuery]int sellerId,[FromQuery] int orderId)
    {
        var invoice = await _sellerOrderService.CreateInvoiceDraftAsync(orderId, sellerId);
        return Ok(invoice); // Oluşturulan fatura taslağını geri döner
    }

    [HttpPut("/invoices/confirm")]
    public async Task<IActionResult> ConfirmInvoice([FromQuery]int sellerId, [FromQuery]int invoiceId)
    {
        await _sellerOrderService.ConfirmInvoiceAndOrderAsync(invoiceId, sellerId);
        return Ok(new { Message = "Fatura onaylandı. Sipariş 'Invoiced' durumuna geçti." });
    }

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