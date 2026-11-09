using AmeWatches.Api.Services.Admin;
using AmeWatches.Api.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmeWatches.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = Roles.Admin)]
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminOrderService _orders;
    public AdminDashboardController(IAdminOrderService orders) => _orders = orders;

    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await _orders.GetSummary());
}
