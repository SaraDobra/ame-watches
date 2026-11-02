using AmeWatches.Api.Data;
using AmeWatches.Api.Models.Payments;
using AmeWatches.Api.Services;
using AmeWatches.Api.Services.Payments;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AmeWatches.Tests;

public class TestTimeProvider : TimeProvider
{
    public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
    public override DateTimeOffset GetUtcNow() => Now;
}

public class PaymentTestKit
{
    public SimulatorPaymentProvider Provider { get; }
    public PaymentService Payments { get; }
    public OrderService Orders { get; }
    public TestTimeProvider Time { get; } = new();
    public PaymentOptions Options { get; } = new() { ExpiryMinutes = 30, WebhookToleranceSeconds = 300, WebhookSecret = "test-secret" };

    public PaymentTestKit(AppDbContext db, IPaymentProvider? providerOverride = null)
    {
        Provider = new SimulatorPaymentProvider(Microsoft.Extensions.Options.Options.Create(Options));
        Payments = new PaymentService(db, providerOverride ?? Provider, Microsoft.Extensions.Options.Options.Create(Options), Time, NullLogger<PaymentService>.Instance);
        Orders = new OrderService(db, Payments, NullLogger<OrderService>.Instance);
    }
}
