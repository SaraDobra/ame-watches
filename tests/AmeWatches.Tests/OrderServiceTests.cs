using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Orders;
using AmeWatches.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace AmeWatches.Tests;

public class OrderServiceTests
{
    private static async Task<(TestDb db, OrderService service)> Setup()
    {
        var db = new TestDb();
        await CatalogFixture.ApplyAsync(db.Context);
        return (db, new PaymentTestKit(db.Context).Orders);
    }

    private static CreateOrderRequest Request(params (int productId, int quantity)[] lines)
        => new("  Arben Krasniqi ", "+383 44 123 456", "arben@mail.com", "Rruga e Dëshmorëve 12", "Prishtinë",
               PaymentMethod.CashOnDelivery, lines.Select(l => new OrderItemRequest(l.productId, l.quantity)).ToList());

    [Fact]
    public async Task CreateOrder_ComputesTotalsFromDatabasePrices_AndStoresASnapshot()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var first = await db.Context.Products.OrderBy(p => p.Id).FirstAsync();
        var second = await db.Context.Products.OrderBy(p => p.Id).Skip(1).FirstAsync();

        var result = await service.CreateOrder(Request((first.Id, 2), (second.Id, 1)));

        Assert.Null(result.Failure);
        var expected = first.Price * 2 + second.Price;
        Assert.Equal(expected, result.Created!.Total);

        var order = await db.Context.Orders.Include(o => o.Items).SingleAsync();
        Assert.Equal(expected, order.Subtotal);
        Assert.Equal(0m, order.Shipping);
        Assert.Equal(OrderStatus.New, order.Status);
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
        Assert.Equal("Arben Krasniqi", order.CustomerName);
        Assert.Equal(2, order.Items.Count);
        Assert.Equal(first.Price, order.Items.Single(i => i.ProductId == first.Id).UnitPrice);
        Assert.Equal(first.Name, order.Items.Single(i => i.ProductId == first.Id).ProductName);
    }

    [Fact]
    public async Task CreateOrder_DecrementsStock()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.FirstAsync();
        var before = product.Stock;

        await service.CreateOrder(Request((product.Id, 3)));

        var stock = await db.Context.Products.AsNoTracking().Where(p => p.Id == product.Id).Select(p => p.Stock).SingleAsync();
        Assert.Equal(before - 3, stock);
    }

    [Fact]
    public async Task CreateOrder_NotEnoughStock_ReturnsConflictAndChangesNothing()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.FirstAsync();
        product.Stock = 2;
        await db.Context.SaveChangesAsync();

        var result = await service.CreateOrder(Request((product.Id, 3)));

        Assert.Equal(OrderFailure.OutOfStock, result.Failure);
        Assert.Equal(0, await db.Context.Orders.CountAsync());
        Assert.Equal(2, await db.Context.Products.AsNoTracking().Where(p => p.Id == product.Id).Select(p => p.Stock).SingleAsync());
    }

    [Fact]
    public async Task CreateOrder_FailureOnSecondLine_RollsBackTheFirstLinesStock()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var products = await db.Context.Products.OrderBy(p => p.Id).Take(2).ToListAsync();
        var firstStock = products[0].Stock;
        products[1].Stock = 0;
        await db.Context.SaveChangesAsync();

        var result = await service.CreateOrder(Request((products[0].Id, 1), (products[1].Id, 1)));

        Assert.Equal(OrderFailure.OutOfStock, result.Failure);
        Assert.Equal(firstStock, await db.Context.Products.AsNoTracking().Where(p => p.Id == products[0].Id).Select(p => p.Stock).SingleAsync());
        Assert.Equal(0, await db.Context.Orders.CountAsync());
    }

    [Fact]
    public async Task CreateOrder_LastPiece_CanOnlyBeBoughtOnce()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.FirstAsync();
        product.Stock = 1;
        await db.Context.SaveChangesAsync();

        var first = await service.CreateOrder(Request((product.Id, 1)));
        var second = await service.CreateOrder(Request((product.Id, 1)));

        Assert.Null(first.Failure);
        Assert.Equal(OrderFailure.OutOfStock, second.Failure);
        Assert.Equal(1, await db.Context.Orders.CountAsync());
    }

    [Fact]
    public async Task CreateOrder_UnknownOrInactiveProduct_IsRejected()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.FirstAsync();
        product.IsActive = false;
        await db.Context.SaveChangesAsync();

        var inactive = await service.CreateOrder(Request((product.Id, 1)));
        var unknown = await service.CreateOrder(Request((99999, 1)));

        Assert.Equal(OrderFailure.InvalidItems, inactive.Failure);
        Assert.Equal(OrderFailure.InvalidItems, unknown.Failure);
        Assert.Equal(0, await db.Context.Orders.CountAsync());
    }

    [Fact]
    public async Task CreateOrder_RepeatedProductLines_AreMergedAndStillLimited()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.FirstAsync();
        product.Stock = 50;
        await db.Context.SaveChangesAsync();

        var ok = await service.CreateOrder(Request((product.Id, 2), (product.Id, 3)));
        var tooMany = await service.CreateOrder(Request((product.Id, 6), (product.Id, 6)));

        Assert.Null(ok.Failure);
        var item = await db.Context.OrderItems.SingleAsync();
        Assert.Equal(5, item.Quantity);
        Assert.Equal(OrderFailure.InvalidItems, tooMany.Failure);
    }

    [Fact]
    public async Task CreateOrder_OrderNumbersAreUniqueAndReadable()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.FirstAsync();
        product.Stock = 100;
        await db.Context.SaveChangesAsync();

        var numbers = new List<string>();
        for (var i = 0; i < 25; i++)
            numbers.Add((await service.CreateOrder(Request((product.Id, 1)))).Created!.OrderNo);

        Assert.Equal(25, numbers.Distinct().Count());
        Assert.All(numbers, n => Assert.Matches("^AW-[A-HJ-NP-Z2-9]{6}$", n));
    }

    [Fact]
    public async Task GetOrder_NeedsTheCorrectToken()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.FirstAsync();
        var created = (await service.CreateOrder(Request((product.Id, 1)))).Created!;

        Assert.NotNull(await service.GetOrder(created.OrderNo, created.AccessToken));
        Assert.Null(await service.GetOrder(created.OrderNo, "gabim"));
        Assert.Null(await service.GetOrder(created.OrderNo, ""));
        Assert.Null(await service.GetOrder("AW-NUKEKZ", created.AccessToken));
    }

    [Fact]
    public async Task GetOrder_ReturnsLinesWithTheirTotals()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.FirstAsync();
        var created = (await service.CreateOrder(Request((product.Id, 2)))).Created!;

        var order = await service.GetOrder(created.OrderNo, created.AccessToken);

        var line = Assert.Single(order!.Items);
        Assert.Equal(2, line.Quantity);
        Assert.Equal(product.Price * 2, line.LineTotal);
        Assert.Equal("CashOnDelivery", order.PaymentMethod);
        Assert.Equal("New", order.Status);
    }

    [Fact]
    public async Task CreateOrder_BlankEmail_IsStoredAsNull()
    {
        var (db, service) = await Setup();
        using var _ = db;
        var product = await db.Context.Products.FirstAsync();
        var request = Request((product.Id, 1)) with { Email = "   " };

        await service.CreateOrder(request);

        Assert.Null((await db.Context.Orders.SingleAsync()).Email);
    }
}
