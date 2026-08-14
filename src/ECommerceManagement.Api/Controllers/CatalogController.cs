using ECommerceManagement.Application.Common;
using ECommerceManagement.Application.DTOs.Catalog;
using ECommerceManagement.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerceManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("GeneralPolicy")]
public class CatalogController : ControllerBase
{
    private readonly ICatalogService _catalogService;
    private readonly ICustomerOrderService _customerOrderService;

    public CatalogController(ICatalogService catalogService,  ICustomerOrderService customerOrderService)
    {
        _catalogService = catalogService;
        _customerOrderService = customerOrderService;
    }

    /// <summary>
    /// Aktif ürünleri dinamik sayfalama, filtreleme ve sıralama ile getirir.
    /// GET: api/catalog/products?pageNumber=1&pageSize=10&searchTerm=laptop&minPrice=1000&sortBy=price&sortOrder=asc
    /// </summary>
    [HttpGet("products")]
    public async Task<ActionResult<PagedResultDto<ProductDto>>> GetActiveProducts([FromQuery] ProductFilterDto filter)
    {
        var result = await _catalogService.GetActiveProductsPagedAsync(filter);
        return Ok(result);
    }
    
    [HttpPost("sync-products-from-sysmond")]
    public async Task<IActionResult> SyncProducts()
    {
        try 
        {
            await _catalogService.SyncProductsAsync();
            return Ok(new { message = "Ürünler Sysmond ile başarıyla senkronize edildi." });
        }
        catch (Exception ex)
        {
            // Detaylı hatayı log'a yazdır ve döndür
            var errorMessage = ex.Message;
            if (ex.InnerException != null)
                errorMessage += " | Detay: " + ex.InnerException.Message;

            return BadRequest(new { message = "Senkronizasyon başarısız oldu.", error = errorMessage });
        }
    }
    
    [HttpPost("sync-orders-from-sysmond")]
    public async Task<IActionResult> SyncOrders()
    {
        await _customerOrderService.SyncOrdersFromSysmondAsync();
        return Ok("Siparişler başarıyla senkronize edildi!");
    }
}