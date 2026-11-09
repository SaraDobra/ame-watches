using AmeWatches.Api.Models.Admin;
using AmeWatches.Api.Services.Admin;
using AmeWatches.Api.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmeWatches.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = Roles.Admin)]
public class AdminCategoriesController : ControllerBase
{
    private readonly IAdminCategoryService _categories;
    public AdminCategoriesController(IAdminCategoryService categories) => _categories = categories;

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await _categories.List());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoryInput input)
    {
        var result = await _categories.Create(input);
        return result.Succeeded ? StatusCode(StatusCodes.Status201Created, result.Value) : ToError(result.Error, result.Message);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Rename(int id, [FromBody] RenameCategoryRequest request)
    {
        var result = await _categories.Rename(id, request.Name);
        return result.Succeeded ? Ok(result.Value) : ToError(result.Error, result.Message);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _categories.Delete(id);
        return result.Succeeded ? NoContent() : ToError(result.Error, result.Message);
    }

    private IActionResult ToError(AdminError? error, string? message) => error switch
    {
        AdminError.NotFound => NotFound(new { message }),
        AdminError.Conflict => Conflict(new { message }),
        _ => BadRequest(new { message })
    };
}
