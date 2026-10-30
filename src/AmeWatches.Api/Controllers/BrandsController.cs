using AmeWatches.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AmeWatches.Api.Controllers;

[ApiController]
[Route("api/brands")]
public class BrandsController : ControllerBase
{
    private readonly ICatalogService _catalog;
    public BrandsController(ICatalogService catalog) => _catalog = catalog;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _catalog.GetBrands());
}
