using AmeWatches.Api.Models.Admin;
using AmeWatches.Api.Services.Admin;
using AmeWatches.Api.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmeWatches.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = Roles.Admin)]
public class AdminProductsController : ControllerBase
{
    private readonly IAdminProductService _products;
    private readonly IAdminImageService _images;

    public AdminProductsController(IAdminProductService products, IAdminImageService images)
    {
        _products = products;
        _images = images;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] AdminProductQuery query)
        => Ok(await _products.List(query));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var product = await _products.Get(id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductInput input)
    {
        var result = await _products.Create(input);
        return result.Succeeded ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value) : ToError(result.Error, result.Message);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductInput input)
    {
        var result = await _products.Update(id, input);
        return result.Succeeded ? Ok(result.Value) : ToError(result.Error, result.Message);
    }

    [HttpPatch("{id:int}/active")]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetActiveRequest request)
    {
        var result = await _products.SetActive(id, request.IsActive);
        return result.Succeeded ? Ok(result.Value) : ToError(result.Error, result.Message);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _products.Delete(id);
        return result.Succeeded ? NoContent() : ToError(result.Error, result.Message);
    }

    [HttpPost("{id:int}/images")]
    [RequestSizeLimit(45 * 1024 * 1024)]
    public async Task<IActionResult> UploadImages(int id, [FromForm] List<IFormFile> files, CancellationToken cancellationToken)
    {
        var uploads = files.Select(f => new UploadedFile(f.FileName, f.Length, f.OpenReadStream)).ToList();
        var result = await _images.Add(id, uploads, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : ToError(result.Error, result.Message);
    }

    [HttpDelete("{id:int}/images/{imageId:int}")]
    public async Task<IActionResult> DeleteImage(int id, int imageId)
    {
        var result = await _images.Delete(id, imageId);
        return result.Succeeded ? Ok(result.Value) : ToError(result.Error, result.Message);
    }

    [HttpPut("{id:int}/images/order")]
    public async Task<IActionResult> ReorderImages(int id, [FromBody] ReorderImagesRequest request)
    {
        var result = await _images.Reorder(id, request.ImageIds);
        return result.Succeeded ? Ok(result.Value) : ToError(result.Error, result.Message);
    }

    private IActionResult ToError(AdminError? error, string? message) => error switch
    {
        AdminError.NotFound => NotFound(new { message }),
        AdminError.Conflict => Conflict(new { message }),
        _ => BadRequest(new { message })
    };
}
