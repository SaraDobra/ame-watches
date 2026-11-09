using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Orders;
using Microsoft.EntityFrameworkCore;

namespace AmeWatches.Tests;

public class CardDisabledTests
{
    private static CreateOrderRequest Request(int productId, PaymentMethod method)
        => new("Arben Krasniqi", "+383 44 123 456", "arben@mail.com", "Rruga e Dëshmorëve 12", "Prishtinë",
               method, [new OrderItemRequest(productId, 1)]);

    private static async Task<(TestDb db, PaymentTestKit kit, Product product)> Setup()
    {
        var db = new TestDb();
        await CatalogFixture.ApplyAsync(db.Context);
        var kit = new PaymentTestKit(db.Context);
        kit.Options.Provider = "Disabled";
        return (db, kit, await db.Context.Products.OrderBy(p => p.Id).FirstAsync());
    }

    [Fact]
    public async Task CardOrder_IsRefusedBeforeAnythingIsSavedOrReserved()
    {
        var (db, kit, product) = await Setup();
        using var _ = db;
        var stock = product.Stock;

        var result = await kit.Orders.CreateOrder(Request(product.Id, PaymentMethod.Card));

        Assert.Equal(OrderFailure.PaymentUnavailable, result.Failure);
        Assert.Empty(await db.Context.Orders.ToListAsync());
        Assert.Equal(stock, await db.Context.Products.Where(p => p.Id == product.Id).Select(p => p.Stock).SingleAsync());
    }

    [Fact]
    public async Task CashOnDelivery_StillWorks()
    {
        var (db, kit, product) = await Setup();
        using var _ = db;

        var result = await kit.Orders.CreateOrder(Request(product.Id, PaymentMethod.CashOnDelivery));

        Assert.Null(result.Failure);
        Assert.False(kit.Payments.CardPaymentsEnabled);
    }
}
