namespace AmeWatches.Api.Services.Payments;

public enum PaymentOutcome
{
    Paid = 1,
    Failed = 2
}

public record PaymentRequest(string OrderNo, string AccessToken, decimal Amount, string Currency, string Description);

public record PaymentSession(string SessionId, string RedirectUrl);

public record PaymentEvent(string SessionId, PaymentOutcome Outcome, decimal Amount, string Currency, DateTimeOffset OccurredAt);

public interface IPaymentProvider
{
    Task<PaymentSession> CreateSession(PaymentRequest request);
    PaymentEvent? ParseWebhook(string body, string? signature);
}
