using AmeWatches.Api.Models.Catalog;
using AmeWatches.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AmeWatches.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ICatalogService _catalog;
    public ProductsController(ICatalogService catalog) => _catalog = catalog;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ProductQuery query)
    {
        if (query.MinPrice > query.MaxPrice)
            return BadRequest(new { message = "MinPrice nuk mund të jetë më i madh se MaxPrice." });

        return Ok(await _catalog.GetProducts(query));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var product = await _catalog.GetProductBySlug(slug);
        return product is null ? NotFound() : Ok(product);
    }
}
