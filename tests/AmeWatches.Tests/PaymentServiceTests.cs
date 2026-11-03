using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Orders;
using AmeWatches.Api.Services.Payments;
using Microsoft.EntityFrameworkCore;

namespace AmeWatches.Tests;

public class PaymentServiceTests
{
    private sealed class BrokenProvider : IPaymentProvider
    {
        public Task<PaymentSession> CreateSession(PaymentRequest request) => throw new InvalidOperationException("provider down");
        public PaymentEvent? ParseWebhook(string body, string? signature) => null;
    }

    private static async Task<(TestDb db, PaymentTestKit kit)> Setup()
    {
        var db = new TestDb();
        await CatalogFixture.ApplyAsync(db.Context);
        return (db, new PaymentTestKit(db.Context));
    }

    private static CreateOrderRequest CardRequest(int productId, int quantity = 1)
        => new("Arben Krasniqi", "+383 44 123 456", "arben@mail.com", "Rruga e Dëshmorëve 12", "Prishtinë",
               PaymentMethod.Card, [new OrderItemRequest(productId, quantity)]);

    private static async Task<(Order order, string sessionId)> PlaceCardOrder(TestDb db, PaymentTestKit kit, int quantity = 1)
    {
        var product = await db.Context.Products.OrderBy(p => p.Id).FirstAsync();
        var result = await kit.Orders.CreateOrder(CardRequest(product.Id, quantity));
        Assert.Null(result.Failure);
        var order = await db.Context.Orders.Include(o => o.Items).SingleAsync(o => o.OrderNo == result.Created!.OrderNo);
        return (order, order.PaymentSessionId!);
    }

    private static Task<WebhookResult> Send(PaymentTestKit kit, string sessionId, PaymentOutcome outcome, decimal amount, DateTimeOffset? at = null)
    {
        var (body, signature) = kit.Provider.BuildWebhook(sessionId, outcome, amount, "EUR", at ?? kit.Time.Now);
        return kit.Payments.HandleWebhook(body, signature);
    }

    private static Task<int> StockOf(TestDb db, int productId)
        => db.Context.Products.AsNoTracking().Where(p => p.Id == productId).Select(p => p.Stock).SingleAsync();

    [Fact]
    public async Task CardOrder_ReservesStockAndReturnsAPaymentUrl()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.OrderBy(p => p.Id).FirstAsync();
        var before = product.Stock;

        var result = await kit.Orders.CreateOrder(CardRequest(product.Id, 2));

        Assert.Null(result.Failure);
        Assert.StartsWith("http://localhost:4200/pagesa/simulator/sim_", result.Created!.PaymentUrl);
        Assert.Contains($"order={result.Created.OrderNo}", result.Created.PaymentUrl);
        var order = await db.Context.Orders.SingleAsync();
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
        Assert.Equal(OrderStatus.New, order.Status);
        Assert.StartsWith("sim_", order.PaymentSessionId);
        Assert.Equal(before - 2, await StockOf(db, product.Id));
    }

    [Fact]
    public async Task CashOrder_HasNoPaymentUrl()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.FirstAsync();
        var request = CardRequest(product.Id) with { PaymentMethod = PaymentMethod.CashOnDelivery };

        var result = await kit.Orders.CreateOrder(request);

        Assert.Null(result.Created!.PaymentUrl);
        Assert.Null((await db.Context.Orders.SingleAsync()).PaymentSessionId);
    }

    [Fact]
    public async Task ProviderFailure_CancelsTheOrderAndReturnsTheStock()
    {
        using var db = new TestDb();
        await CatalogFixture.ApplyAsync(db.Context);
        var kit = new PaymentTestKit(db.Context, new BrokenProvider());
        var product = await db.Context.Products.OrderBy(p => p.Id).FirstAsync();
        var before = product.Stock;

        var result = await kit.Orders.CreateOrder(CardRequest(product.Id, 2));

        Assert.Equal(OrderFailure.PaymentUnavailable, result.Failure);
        var order = await db.Context.Orders.SingleAsync();
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(before, await StockOf(db, product.Id));
    }

    [Fact]
    public async Task PaidWebhook_ConfirmsTheOrder()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var (order, session) = await PlaceCardOrder(db, kit);

        var result = await Send(kit, session, PaymentOutcome.Paid, order.Total);

        Assert.Equal(WebhookResult.Accepted, result);
        var saved = await db.Context.Orders.AsNoTracking().SingleAsync();
        Assert.Equal(PaymentStatus.Paid, saved.PaymentStatus);
        Assert.Equal(OrderStatus.Confirmed, saved.Status);
        Assert.NotNull(saved.PaidAt);
    }

    [Fact]
    public async Task PaidWebhook_SentTwice_ChangesNothingTheSecondTime()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var (order, session) = await PlaceCardOrder(db, kit);
        await Send(kit, session, PaymentOutcome.Paid, order.Total);
        var firstPaidAt = (await db.Context.Orders.AsNoTracking().SingleAsync()).PaidAt;
        kit.Time.Now = kit.Time.Now.AddMinutes(1);

        var second = await Send(kit, session, PaymentOutcome.Paid, order.Total);

        Assert.Equal(WebhookResult.Accepted, second);
        Assert.Equal(firstPaidAt, (await db.Context.Orders.AsNoTracking().SingleAsync()).PaidAt);
    }

    [Fact]
    public async Task FailedWebhook_CancelsTheOrderAndRestoresStock()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.OrderBy(p => p.Id).FirstAsync();
        var before = product.Stock;
        var (order, session) = await PlaceCardOrder(db, kit, 2);

        var result = await Send(kit, session, PaymentOutcome.Failed, order.Total);

        Assert.Equal(WebhookResult.Accepted, result);
        var saved = await db.Context.Orders.AsNoTracking().SingleAsync();
        Assert.Equal(OrderStatus.Cancelled, saved.Status);
        Assert.Equal(PaymentStatus.Failed, saved.PaymentStatus);
        Assert.Equal(before, await StockOf(db, product.Id));
    }

    [Fact]
    public async Task FailedWebhook_SentTwice_DoesNotRestoreStockTwice()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.OrderBy(p => p.Id).FirstAsync();
        var before = product.Stock;
        var (order, session) = await PlaceCardOrder(db, kit);

        await Send(kit, session, PaymentOutcome.Failed, order.Total);
        await Send(kit, session, PaymentOutcome.Failed, order.Total);

        Assert.Equal(before, await StockOf(db, product.Id));
    }

    [Fact]
    public async Task FailedWebhook_AfterPayment_IsIgnored()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var (order, session) = await PlaceCardOrder(db, kit);
        await Send(kit, session, PaymentOutcome.Paid, order.Total);

        var result = await Send(kit, session, PaymentOutcome.Failed, order.Total);

        Assert.Equal(WebhookResult.Ignored, result);
        Assert.Equal(PaymentStatus.Paid, (await db.Context.Orders.AsNoTracking().SingleAsync()).PaymentStatus);
    }

    [Fact]
    public async Task Webhook_WithABadSignatureOrATamperedBody_IsRejected()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var (order, session) = await PlaceCardOrder(db, kit);
        var (body, signature) = kit.Provider.BuildWebhook(session, PaymentOutcome.Paid, order.Total, "EUR", kit.Time.Now);

        Assert.Equal(WebhookResult.Invalid, await kit.Payments.HandleWebhook(body, "000000"));
        Assert.Equal(WebhookResult.Invalid, await kit.Payments.HandleWebhook(body, null));
        Assert.Equal(WebhookResult.Invalid, await kit.Payments.HandleWebhook(body.Replace("Paid", "Failed"), signature));
        Assert.Equal(PaymentStatus.Pending, (await db.Context.Orders.AsNoTracking().SingleAsync()).PaymentStatus);
    }

    [Fact]
    public async Task Webhook_WithAWrongAmount_IsIgnored()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var (order, session) = await PlaceCardOrder(db, kit);

        var result = await Send(kit, session, PaymentOutcome.Paid, 1m);

        Assert.Equal(WebhookResult.Ignored, result);
        Assert.Equal(PaymentStatus.Pending, (await db.Context.Orders.AsNoTracking().SingleAsync()).PaymentStatus);
    }

    [Fact]
    public async Task Webhook_ThatIsTooOld_IsRejectedAsAReplay()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var (order, session) = await PlaceCardOrder(db, kit);

        var result = await Send(kit, session, PaymentOutcome.Paid, order.Total, kit.Time.Now.AddMinutes(-10));

        Assert.Equal(WebhookResult.Invalid, result);
    }

    [Fact]
    public async Task Webhook_ForAnUnknownSession_IsNotFound()
    {
        var (db, kit) = await Setup();
        using var _ = db;

        Assert.Equal(WebhookResult.NotFound, await Send(kit, "sim_unknown", PaymentOutcome.Paid, 10m));
    }

    [Fact]
    public async Task UnpaidCardOrders_AreCancelledAfterTheExpiry_AndStockComesBack()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.OrderBy(p => p.Id).FirstAsync();
        var before = product.Stock;
        var (order, _) = await PlaceCardOrder(db, kit, 2);
        order.CreatedAt = kit.Time.Now.UtcDateTime.AddMinutes(-31);
        await db.Context.SaveChangesAsync();

        var expired = await kit.Payments.ExpireUnpaidOrders(kit.Time.Now.AddMinutes(-kit.Options.ExpiryMinutes));

        Assert.Equal(1, expired);
        var saved = await db.Context.Orders.AsNoTracking().SingleAsync();
        Assert.Equal(OrderStatus.Cancelled, saved.Status);
        Assert.Equal(PaymentStatus.Failed, saved.PaymentStatus);
        Assert.Equal(before, await StockOf(db, product.Id));
    }

    [Fact]
    public async Task Expiry_LeavesRecentPaidAndCashOrdersAlone()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var products = await db.Context.Products.OrderBy(p => p.Id).Take(3).ToListAsync();

        var recent = (await kit.Orders.CreateOrder(CardRequest(products[0].Id))).Created!;
        var paid = (await kit.Orders.CreateOrder(CardRequest(products[1].Id))).Created!;
        var cash = (await kit.Orders.CreateOrder(CardRequest(products[2].Id) with { PaymentMethod = PaymentMethod.CashOnDelivery })).Created!;

        var paidOrder = await db.Context.Orders.SingleAsync(o => o.OrderNo == paid.OrderNo);
        await Send(kit, paidOrder.PaymentSessionId!, PaymentOutcome.Paid, paidOrder.Total);

        var old = kit.Time.Now.UtcDateTime.AddHours(-5);
        foreach (var o in await db.Context.Orders.ToListAsync()) o.CreatedAt = o.OrderNo == recent.OrderNo ? kit.Time.Now.UtcDateTime : old;
        await db.Context.SaveChangesAsync();

        var expired = await kit.Payments.ExpireUnpaidOrders(kit.Time.Now.AddMinutes(-30));

        Assert.Equal(0, expired);
        Assert.DoesNotContain(await db.Context.Orders.AsNoTracking().ToListAsync(), o => o.Status == OrderStatus.Cancelled);
    }

    [Fact]
    public async Task PaymentArrivingAfterExpiry_ReopensTheOrder_WhenStockIsStillThere()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.OrderBy(p => p.Id).FirstAsync();
        var before = product.Stock;
        var (order, session) = await PlaceCardOrder(db, kit);
        await kit.Payments.CancelAndRestock(order.Id);

        var result = await Send(kit, session, PaymentOutcome.Paid, order.Total);

        Assert.Equal(WebhookResult.Accepted, result);
        var saved = await db.Context.Orders.AsNoTracking().SingleAsync();
        Assert.Equal(OrderStatus.Confirmed, saved.Status);
        Assert.Equal(PaymentStatus.Paid, saved.PaymentStatus);
        Assert.Equal(before - 1, await StockOf(db, product.Id));
    }

    [Fact]
    public async Task PaymentArrivingAfterExpiry_WithoutStock_StaysCancelledButIsMarkedPaid()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.OrderBy(p => p.Id).FirstAsync();
        var (order, session) = await PlaceCardOrder(db, kit);
        await kit.Payments.CancelAndRestock(order.Id);
        var tracked = await db.Context.Products.SingleAsync(p => p.Id == product.Id);
        tracked.Stock = 0;
        await db.Context.SaveChangesAsync();

        await Send(kit, session, PaymentOutcome.Paid, order.Total);

        var saved = await db.Context.Orders.AsNoTracking().SingleAsync();
        Assert.Equal(OrderStatus.Cancelled, saved.Status);
        Assert.Equal(PaymentStatus.Paid, saved.PaymentStatus);
        Assert.Equal(0, await StockOf(db, product.Id));
    }

    [Fact]
    public async Task SimulatorSignatures_AreNotInterchangeableBetweenInstancesWithDifferentSecrets()
    {
        var (db, kit) = await Setup();
        using var _ = db;
        var (order, session) = await PlaceCardOrder(db, kit);
        var other = new PaymentTestKit(db.Context);
        other.Options.WebhookSecret = "another-secret";
        var foreign = new SimulatorPaymentProvider(Microsoft.Extensions.Options.Options.Create(other.Options));
        var (body, signature) = foreign.BuildWebhook(session, PaymentOutcome.Paid, order.Total, "EUR", kit.Time.Now);

        Assert.Equal(WebhookResult.Invalid, await kit.Payments.HandleWebhook(body, signature));
    }
}
