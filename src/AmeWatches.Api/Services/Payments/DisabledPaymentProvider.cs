namespace AmeWatches.Api.Services.Payments;

/// <summary>Përdoret kur pagesa me kartë është e fikur (p.sh. para se të ketë ofrues real). Mbetet vetëm pagesa në dorëzim.</summary>
public class DisabledPaymentProvider : IPaymentProvider
{
    public Task<PaymentSession> CreateSession(PaymentRequest request)
        => throw new InvalidOperationException("Pagesa me kartë është e fikur.");

    public PaymentEvent? ParseWebhook(string body, string? signature) => null;
}
