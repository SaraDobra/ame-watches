using AmeWatches.Api.Data;
using AmeWatches.Api.Models.Email;
using AmeWatches.Api.Models.Payments;
using AmeWatches.Api.Services.Email;
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

public sealed class RecordingOutbox : IEmailOutbox
{
    public List<EmailMessage> Messages { get; } = [];

    public bool Enqueue(EmailMessage message)
    {
        Messages.Add(message);
        return true;
    }
}

public class PaymentTestKit
{
    public SimulatorPaymentProvider Provider { get; }
    public PaymentService Payments { get; }
    public OrderService Orders { get; }
    public TestTimeProvider Time { get; } = new();
    public RecordingOutbox Emails { get; } = new();
    public EmailOptions EmailSettings { get; } = new() { PublicBaseUrl = "https://ame.test", FromAddress = "ame.accessories.ks@gmail.com", ShopAddress = "ame.accessories.ks@gmail.com" };
    public PaymentOptions Options { get; } = new() { ExpiryMinutes = 30, WebhookToleranceSeconds = 300, WebhookSecret = "test-secret" };

    public PaymentTestKit(AppDbContext db, IPaymentProvider? providerOverride = null)
    {
        Provider = new SimulatorPaymentProvider(Microsoft.Extensions.Options.Options.Create(Options));
        var notifier = new OrderNotifier(Emails, new OrderEmailBuilder(Microsoft.Extensions.Options.Options.Create(EmailSettings)), NullLogger<OrderNotifier>.Instance);
        Payments = new PaymentService(db, providerOverride ?? Provider, Microsoft.Extensions.Options.Options.Create(Options), Time, notifier, NullLogger<PaymentService>.Instance);
        Orders = new OrderService(db, Payments, notifier, NullLogger<OrderService>.Instance);
    }
}
