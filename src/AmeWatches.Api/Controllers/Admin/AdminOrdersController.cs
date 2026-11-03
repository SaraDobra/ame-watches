using AmeWatches.Api.Models.Admin;
using AmeWatches.Api.Services.Admin;
using AmeWatches.Api.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmeWatches.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = Roles.Admin)]
public class AdminOrdersController : ControllerBase
{
    private readonly IAdminOrderService _orders;
    public AdminOrdersController(IAdminOrderService orders) => _orders = orders;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] AdminOrderQuery query)
        => Ok(await _orders.List(query));

    [HttpGet("{orderNo}")]
    public async Task<IActionResult> Get(string orderNo)
    {
        var order = await _orders.Get(orderNo);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPut("{orderNo}/status")]
    public async Task<IActionResult> ChangeStatus(string orderNo, [FromBody] ChangeStatusRequest request)
    {
        var result = await _orders.ChangeStatus(orderNo, request.Status);

        return result.Error switch
        {
            null => Ok(result.Value),
            AdminError.NotFound => NotFound(new { message = result.Message }),
            AdminError.Conflict => Conflict(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }
}
