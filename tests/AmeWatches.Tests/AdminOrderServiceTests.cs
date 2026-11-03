using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Admin;
using AmeWatches.Api.Models.Orders;
using AmeWatches.Api.Services.Admin;
using AmeWatches.Api.Services.Payments;
using Microsoft.EntityFrameworkCore;

namespace AmeWatches.Tests;

public class AdminOrderServiceTests
{
    private sealed record Kit(TestDb Db, PaymentTestKit Payments, AdminOrderService Admin);

    private static async Task<Kit> Setup()
    {
        var db = new TestDb();
        await CatalogFixture.ApplyAsync(db.Context);
        var payments = new PaymentTestKit(db.Context);
        return new Kit(db, payments, new AdminOrderService(db.Context, payments.Payments, payments.Time));
    }

    private static CreateOrderRequest Request(int productId, PaymentMethod method, int quantity = 1)
        => new("Arben Krasniqi", "+383 44 123 456", "arben@mail.com", "Rruga e Dëshmorëve 12", "Prishtinë", method, [new OrderItemRequest(productId, quantity)]);

    private static async Task<string> Place(Kit kit, PaymentMethod method = PaymentMethod.CashOnDelivery, int quantity = 1, int productIndex = 0)
    {
        var product = await kit.Db.Context.Products.OrderBy(p => p.Id).Skip(productIndex).FirstAsync();
        return (await kit.Payments.Orders.CreateOrder(Request(product.Id, method, quantity))).Created!.OrderNo;
    }

    [Fact]
    public async Task CashOrder_CanGoThroughTheWholeLifecycle()
    {
        var kit = await Setup();
        using var _ = kit.Db;
        var orderNo = await Place(kit);

        Assert.True((await kit.Admin.ChangeStatus(orderNo, OrderStatus.Confirmed)).Succeeded);
        Assert.True((await kit.Admin.ChangeStatus(orderNo, OrderStatus.Shipped)).Succeeded);
        var delivered = await kit.Admin.ChangeStatus(orderNo, OrderStatus.Delivered);

        Assert.True(delivered.Succeeded);
        Assert.Equal("Delivered", delivered.Value!.Status);
        Assert.Empty(delivered.Value.AllowedStatuses);
    }

    [Theory]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    public async Task SkippingAStep_IsRefused(OrderStatus target)
    {
        var kit = await Setup();
        using var _ = kit.Db;
        var orderNo = await Place(kit);

        var result = await kit.Admin.ChangeStatus(orderNo, target);

        Assert.Equal(AdminError.Conflict, result.Error);
        Assert.Equal("New", (await kit.Admin.Get(orderNo))!.Status);
    }

    [Fact]
    public async Task FinishedOrders_CannotBeChangedAgain()
    {
        var kit = await Setup();
        using var _ = kit.Db;
        var orderNo = await Place(kit);
        await kit.Admin.ChangeStatus(orderNo, OrderStatus.Cancelled);

        Assert.Equal(AdminError.Conflict, (await kit.Admin.ChangeStatus(orderNo, OrderStatus.Confirmed)).Error);
        Assert.Equal(AdminError.Conflict, (await kit.Admin.ChangeStatus(orderNo, OrderStatus.Cancelled)).Error);
    }

    [Fact]
    public async Task Delivering_SetsWarrantyForOneYear_AndMarksCashAsPaid()
    {
        var kit = await Setup();
        using var _ = kit.Db;
        var orderNo = await Place(kit);
        await kit.Admin.ChangeStatus(orderNo, OrderStatus.Confirmed);
        await kit.Admin.ChangeStatus(orderNo, OrderStatus.Shipped);

        await kit.Admin.ChangeStatus(orderNo, OrderStatus.Delivered);

        var saved = await kit.Db.Context.Orders.AsNoTracking().SingleAsync();
        Assert.Equal(kit.Payments.Time.Now.UtcDateTime.AddYears(1), saved.WarrantyUntil);
        Assert.Equal(PaymentStatus.Paid, saved.PaymentStatus);
        Assert.NotNull(saved.PaidAt);
    }

    [Fact]
    public async Task Cancelling_ReturnsTheStock()
    {
        var kit = await Setup();
        using var _ = kit.Db;
        var product = await kit.Db.Context.Products.OrderBy(p => p.Id).FirstAsync();
        var before = product.Stock;
        var orderNo = await Place(kit, quantity: 3);
        Assert.Equal(before - 3, await kit.Db.Context.Products.AsNoTracking().Where(p => p.Id == product.Id).Select(p => p.Stock).SingleAsync());

        await kit.Admin.ChangeStatus(orderNo, OrderStatus.Cancelled);

        Assert.Equal(before, await kit.Db.Context.Products.AsNoTracking().Where(p => p.Id == product.Id).Select(p => p.Stock).SingleAsync());
    }

    [Fact]
    public async Task AnUnpaidCardOrder_CannotBeConfirmedOrShipped_ButCanBeCancelled()
    {
        var kit = await Setup();
        using var _ = kit.Db;
        var orderNo = await Place(kit, PaymentMethod.Card);

        Assert.Equal(AdminError.Conflict, (await kit.Admin.ChangeStatus(orderNo, OrderStatus.Confirmed)).Error);
        Assert.True((await kit.Admin.ChangeStatus(orderNo, OrderStatus.Cancelled)).Succeeded);
    }

    [Fact]
    public async Task CancellingAPaidCardOrder_IsFlaggedForARefund()
    {
        var kit = await Setup();
        using var _ = kit.Db;
        var orderNo = await Place(kit, PaymentMethod.Card);
        var order = await kit.Db.Context.Orders.SingleAsync();
        var (body, signature) = kit.Payments.Provider.BuildWebhook(order.PaymentSessionId!, PaymentOutcome.Paid, order.Total, "EUR", kit.Payments.Time.Now);
        await kit.Payments.Payments.HandleWebhook(body, signature);

        var result = await kit.Admin.ChangeStatus(orderNo, OrderStatus.Cancelled);

        Assert.True(result.Value!.NeedsRefund);
        Assert.Equal("Cancelled", result.Value.Status);
    }

    [Fact]
    public async Task UnknownOrder_IsNotFound()
    {
        var kit = await Setup();
        using var _ = kit.Db;

        Assert.Equal(AdminError.NotFound, (await kit.Admin.ChangeStatus("AW-NUKEKZ", OrderStatus.Confirmed)).Error);
        Assert.Null(await kit.Admin.Get("AW-NUKEKZ"));
    }

    [Fact]
    public async Task List_FiltersByStatus_AndSearchesByNameOrNumber()
    {
        var kit = await Setup();
        using var _ = kit.Db;
        var first = await Place(kit);
        await Place(kit, productIndex: 1);
        await kit.Admin.ChangeStatus(first, OrderStatus.Confirmed);

        var confirmed = await kit.Admin.List(new AdminOrderQuery { Status = OrderStatus.Confirmed });
        var byNumber = await kit.Admin.List(new AdminOrderQuery { Search = first.ToLower() });
        var byName = await kit.Admin.List(new AdminOrderQuery { Search = "krasniqi" });
        var wildcard = await kit.Admin.List(new AdminOrderQuery { Search = "%" });

        Assert.Equal(first, Assert.Single(confirmed.Items).OrderNo);
        Assert.Equal(first, Assert.Single(byNumber.Items).OrderNo);
        Assert.Equal(2, byName.TotalCount);
        Assert.Equal(0, wildcard.TotalCount);
    }

    [Fact]
    public async Task Summary_CountsTheRightThings_AndListsLowStock()
    {
        var kit = await Setup();
        using var _ = kit.Db;
        var cash = await Place(kit);
        await Place(kit, PaymentMethod.Card, productIndex: 1);
        var toShip = await Place(kit, productIndex: 2);
        await kit.Admin.ChangeStatus(toShip, OrderStatus.Confirmed);

        var summary = await kit.Admin.GetSummary();

        Assert.Equal(1, summary.NewOrders);
        Assert.Equal(1, summary.AwaitingPayment);
        Assert.Equal(1, summary.ToShip);
        Assert.Equal(3, summary.OrdersToday);
        Assert.Equal(0m, summary.RevenueThisMonth);
        Assert.Contains(summary.LowStock, p => p.Stock <= AdminOrderService.LowStockThreshold);
        Assert.NotNull(cash);
    }
}
