using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Email;
using AmeWatches.Api.Models.Orders;
using AmeWatches.Api.Services.Email;
using AmeWatches.Api.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AmeWatches.Tests;

public class OrderEmailTests
{
    private const string Shop = "ame.accessories.ks@gmail.com";

    private static async Task<(TestDb db, PaymentTestKit kit)> Setup()
    {
        var db = new TestDb();
        await CatalogFixture.ApplyAsync(db.Context);
        return (db, new PaymentTestKit(db.Context));
    }

    private static CreateOrderRequest Request(int productId, PaymentMethod method, string name = "Arben Krasniqi", string email = "arben@mail.com", int quantity = 2)
        => new(name, "+383 44 123 456", email, "Rruga e Dëshmorëve 12", "Prishtinë", method, [new OrderItemRequest(productId, quantity)]);

    [Fact]
    public async Task CashOrder_SendsAConfirmationToTheCustomerAndANoticeToTheShop()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.OrderBy(p => p.Id).FirstAsync();

        await kit.Orders.CreateOrder(Request(product.Id, PaymentMethod.CashOnDelivery));

        Assert.Equal(2, kit.Emails.Messages.Count);
        var customer = kit.Emails.Messages.Single(m => m.To == "arben@mail.com");
        var shop = kit.Emails.Messages.Single(m => m.To == Shop);
        Assert.Equal(Shop, customer.ReplyTo);
        Assert.Equal("arben@mail.com", shop.ReplyTo);
        Assert.Contains("Konfirmimi i porosisë AW-", customer.Subject);
        Assert.Contains("Porosi e re AW-", shop.Subject);
        Assert.Contains($"{product.Name} × 2", shop.Subject);
        Assert.Contains("Arben Krasniqi", shop.Subject);
    }

    [Fact]
    public async Task TheCustomerEmail_HasTheOrderDetailsAndALinkWithTheAccessToken()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.OrderBy(p => p.Id).FirstAsync();

        await kit.Orders.CreateOrder(Request(product.Id, PaymentMethod.CashOnDelivery));

        var order = await db.Context.Orders.SingleAsync();
        var customer = kit.Emails.Messages.Single(m => m.To == "arben@mail.com");
        Assert.Contains(order.OrderNo, customer.HtmlBody);
        Assert.Contains(product.ModelCode, customer.HtmlBody);
        Assert.Contains($"https://ame.test/porosia/konfirmim/{order.OrderNo}?token={Uri.EscapeDataString(order.AccessToken)}", customer.HtmlBody);
        Assert.Contains("Pagesë në dorëzim", customer.HtmlBody);
        Assert.Contains("1 vit", customer.HtmlBody);
        Assert.Contains(order.OrderNo, customer.TextBody);
        Assert.Contains(order.AccessToken, customer.TextBody);
    }

    [Fact]
    public async Task TheShopEmail_HasThePhoneAddressProductsAndTheAdminLink()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.OrderBy(p => p.Id).FirstAsync();

        await kit.Orders.CreateOrder(Request(product.Id, PaymentMethod.CashOnDelivery));

        var order = await db.Context.Orders.SingleAsync();
        var shop = kit.Emails.Messages.Single(m => m.To == Shop);
        Assert.Contains("+383 44 123 456", shop.HtmlBody);
        Assert.Contains("Rruga e Dëshmorëve 12", shop.HtmlBody);
        Assert.Contains(product.Name, shop.HtmlBody);
        Assert.Contains($"https://ame.test/admin/porosite/{order.OrderNo}", shop.HtmlBody);
        Assert.DoesNotContain(order.AccessToken, shop.HtmlBody);
    }

    [Fact]
    public async Task ACardOrder_SendsNothingUntilItIsPaid_AndThenExactlyOnce()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.OrderBy(p => p.Id).FirstAsync();

        await kit.Orders.CreateOrder(Request(product.Id, PaymentMethod.Card));
        Assert.Empty(kit.Emails.Messages);

        var order = await db.Context.Orders.SingleAsync();
        var (body, signature) = kit.Provider.BuildWebhook(order.PaymentSessionId!, PaymentOutcome.Paid, order.Total, "EUR", kit.Time.Now);
        await kit.Payments.HandleWebhook(body, signature);
        Assert.Equal(2, kit.Emails.Messages.Count);
        Assert.Contains("Me kartë (e paguar)", kit.Emails.Messages[0].HtmlBody);

        await kit.Payments.HandleWebhook(body, signature);
        Assert.Equal(2, kit.Emails.Messages.Count);
    }

    [Fact]
    public async Task AFailedCardPayment_SendsNoConfirmation()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.OrderBy(p => p.Id).FirstAsync();
        await kit.Orders.CreateOrder(Request(product.Id, PaymentMethod.Card));
        var order = await db.Context.Orders.SingleAsync();

        var (body, signature) = kit.Provider.BuildWebhook(order.PaymentSessionId!, PaymentOutcome.Failed, order.Total, "EUR", kit.Time.Now);
        await kit.Payments.HandleWebhook(body, signature);

        Assert.Empty(kit.Emails.Messages);
    }

    [Fact]
    public async Task AnOrderWithoutAnEmail_StillNotifiesTheShop()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.OrderBy(p => p.Id).FirstAsync();
        await kit.Orders.CreateOrder(Request(product.Id, PaymentMethod.CashOnDelivery, email: "   "));

        var message = Assert.Single(kit.Emails.Messages);
        Assert.Equal(Shop, message.To);
        Assert.Null(message.ReplyTo);
    }

    [Fact]
    public async Task HostileInput_IsEscapedInTheBody_AndStrippedFromTheSubject()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.OrderBy(p => p.Id).FirstAsync();

        await kit.Orders.CreateOrder(Request(product.Id, PaymentMethod.CashOnDelivery, name: "Eve <script>alert(1)</script>\r\nBcc: spy@evil.com"));

        foreach (var message in kit.Emails.Messages)
        {
            Assert.DoesNotContain("<script>", message.HtmlBody);
            Assert.DoesNotContain('\r', message.Subject);
            Assert.DoesNotContain('\n', message.Subject);
        }
        Assert.Contains("&lt;script&gt;", kit.Emails.Messages[0].HtmlBody);
    }

    [Fact]
    public async Task TheOrderStillSucceeds_WhenTheEmailsCannotBeBuilt()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var broken = new OrderNotifier(new ThrowingOutbox(), new OrderEmailBuilder(Options.Create(new EmailOptions())), NullLogger<OrderNotifier>.Instance);

        var order = new Order { OrderNo = "AW-TEST", AccessToken = "t", CustomerName = "X", Phone = "1", Email = "x@y.com", Address = "a", City = "c" };
        var error = Record.Exception(() => broken.OrderConfirmed(order));

        Assert.Null(error);
    }

    private sealed class ThrowingOutbox : IEmailOutbox
    {
        public bool Enqueue(EmailMessage message) => throw new InvalidOperationException("kanali u prish");
    }

    private sealed class FlakySender : IEmailSender
    {
        private readonly int _failures;
        public int Calls;
        public List<EmailMessage> Sent { get; } = [];

        public FlakySender(int failures) => _failures = failures;

        public Task Send(EmailMessage message, CancellationToken cancellationToken)
        {
            Calls++;
            if (Calls <= _failures) throw new IOException("Gmail nuk përgjigjet");
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private static async Task<(EmailOutbox outbox, EmailDispatchService service, FlakySender sender, CancellationTokenSource cts)> Dispatcher(int failures)
    {
        var sender = new FlakySender(failures);
        var services = new ServiceCollection().AddSingleton<IEmailSender>(sender).BuildServiceProvider();
        var outbox = new EmailOutbox();
        var service = new EmailDispatchService(outbox, services.GetRequiredService<IServiceScopeFactory>(), NullLogger<EmailDispatchService>.Instance)
        {
            Delays = [TimeSpan.Zero, TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10)]
        };
        var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        return (outbox, service, sender, cts);
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++) await Task.Delay(20);
    }

    private static readonly EmailMessage Sample = new("a@b.com", "A", "Subjekt", "<p>x</p>", "x");

    [Fact]
    public async Task TheQueue_RetriesAfterAFailure_AndEventuallyDelivers()
    {
        var (outbox, service, sender, cts) = await Dispatcher(failures: 2);
        outbox.Enqueue(Sample);

        await WaitUntil(() => sender.Sent.Count == 1);

        Assert.Single(sender.Sent);
        Assert.Equal(3, sender.Calls);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task TheQueue_GivesUpQuietly_AndKeepsWorkingForTheNextEmail()
    {
        var (outbox, service, sender, cts) = await Dispatcher(failures: 3);
        outbox.Enqueue(Sample);
        outbox.Enqueue(Sample with { Subject = "I dyti" });

        await WaitUntil(() => sender.Sent.Count == 1);

        Assert.Equal("I dyti", Assert.Single(sender.Sent).Subject);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task TheFileSender_WritesAReadableHtmlFile()
    {
        var folder = Path.Combine(Path.GetTempPath(), "ame-emails-" + Guid.NewGuid().ToString("N"));
        try
        {
            var sender = new FileEmailSender(Options.Create(new EmailOptions { SinkFolder = folder }),
                new ImageUploadTestsEnvironment(), NullLogger<FileEmailSender>.Instance);

            await sender.Send(Sample, CancellationToken.None);

            var file = Assert.Single(Directory.GetFiles(folder, "*.html"));
            var text = await File.ReadAllTextAsync(file);
            Assert.Contains("To: a@b.com", text);
            Assert.Contains("Subject: Subjekt", text);
            Assert.Contains("<p>x</p>", text);
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }
}

internal sealed class ImageUploadTestsEnvironment : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "t";
    public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    public string WebRootPath { get; set; } = "";
    public string EnvironmentName { get; set; } = "Development";
    public string ContentRootPath { get; set; } = "";
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
}
