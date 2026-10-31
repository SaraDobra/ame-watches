using AmeWatches.Api.Models.Orders;

namespace AmeWatches.Api.Services;

public interface IOrderService
{
    Task<CreateOrderResult> CreateOrder(CreateOrderRequest request);
    Task<OrderResponse?> GetOrder(string orderNo, string token);
}
