using AmeWatches.Api.Models.Orders;
using AmeWatches.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AmeWatches.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;
    public OrdersController(IOrderService orders) => _orders = orders;

    [HttpPost]
    [EnableRateLimiting("orders")]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        var result = await _orders.CreateOrder(request);

        return result.Failure switch
        {
            null => CreatedAtAction(nameof(Get), new { orderNo = result.Created!.OrderNo, token = result.Created.AccessToken }, result.Created),
            OrderFailure.OutOfStock => Conflict(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }

    [HttpGet("{orderNo}")]
    public async Task<IActionResult> Get(string orderNo, [FromQuery] string? token)
    {
        var order = await _orders.GetOrder(orderNo, token ?? string.Empty);
        return order is null ? NotFound() : Ok(order);
    }
}
