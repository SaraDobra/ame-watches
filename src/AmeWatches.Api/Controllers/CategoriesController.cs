using AmeWatches.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AmeWatches.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICatalogService _catalog;
    public CategoriesController(ICatalogService catalog) => _catalog = catalog;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? brand)
    {
        if (brand is { Length: > 100 })
            return BadRequest(new { message = "Brendi nuk është i vlefshëm." });

        return Ok(await _catalog.GetCategories(brand));
    }
}
