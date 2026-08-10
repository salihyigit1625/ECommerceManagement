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

    public CatalogController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
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
}