using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AmeWatches.Api.Models.Payments;
using Microsoft.Extensions.Options;

namespace AmeWatches.Api.Services.Payments;

public class SimulatorPaymentProvider : IPaymentProvider
{
    private record WebhookPayload(string SessionId, string Outcome, decimal Amount, string Currency, DateTimeOffset Timestamp);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly PaymentOptions _options;
    private readonly byte[] _secret;

    public SimulatorPaymentProvider(IOptions<PaymentOptions> options)
    {
        _options = options.Value;
        _secret = string.IsNullOrWhiteSpace(_options.WebhookSecret)
            ? RandomNumberGenerator.GetBytes(32)
            : Encoding.UTF8.GetBytes(_options.WebhookSecret);
    }

    public Task<PaymentSession> CreateSession(PaymentRequest request)
    {
        var sessionId = $"sim_{Guid.NewGuid():N}";
        var url = $"{_options.FrontendBaseUrl.TrimEnd('/')}/pagesa/simulator/{sessionId}" +
                  $"?order={Uri.EscapeDataString(request.OrderNo)}&token={Uri.EscapeDataString(request.AccessToken)}";
        return Task.FromResult(new PaymentSession(sessionId, url));
    }

    public PaymentEvent? ParseWebhook(string body, string? signature)
    {
        if (string.IsNullOrWhiteSpace(signature) || !SignatureMatches(body, signature))
            return null;

        try
        {
            var payload = JsonSerializer.Deserialize<WebhookPayload>(body, Json);
            if (payload is null || string.IsNullOrWhiteSpace(payload.SessionId) || !Enum.TryParse<PaymentOutcome>(payload.Outcome, true, out var outcome))
                return null;

            return new PaymentEvent(payload.SessionId, outcome, payload.Amount, payload.Currency, payload.Timestamp);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public (string Body, string Signature) BuildWebhook(string sessionId, PaymentOutcome outcome, decimal amount, string currency, DateTimeOffset timestamp)
    {
        var body = JsonSerializer.Serialize(new WebhookPayload(sessionId, outcome.ToString(), amount, currency, timestamp), Json);
        return (body, Sign(body));
    }

    public string Sign(string body)
        => Convert.ToHexString(HMACSHA256.HashData(_secret, Encoding.UTF8.GetBytes(body))).ToLowerInvariant();

    private bool SignatureMatches(string body, string signature)
        => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(Sign(body)), Encoding.UTF8.GetBytes(signature.Trim().ToLowerInvariant()));
}
