using AmeWatches.Api.Data.Entities;

namespace AmeWatches.Api.Services.Payments;

public enum WebhookResult
{
    Accepted = 1,
    Ignored = 2,
    Invalid = 3,
    NotFound = 4
}

public record PaymentSessionInfo(string OrderNo, decimal Amount, string Currency, string PaymentStatus);

public interface IPaymentService
{
    Task<PaymentSession> StartCardPayment(Order order);
    Task<WebhookResult> HandleWebhook(string body, string? signature);
    Task<PaymentSessionInfo?> GetSession(string sessionId);
    Task CancelAndRestock(int orderId);
    Task<int> ExpireUnpaidOrders(DateTimeOffset cutoff);
}
