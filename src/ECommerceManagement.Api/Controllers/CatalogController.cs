using ECommerceManagement.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public CatalogController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetActiveProducts()
    {
        var products = await _catalogService.GetActiveProductsAsync();
        return Ok(products);
    }
}