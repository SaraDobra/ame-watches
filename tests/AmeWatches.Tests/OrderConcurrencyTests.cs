using AmeWatches.Api.Data;
using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Orders;
using AmeWatches.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace AmeWatches.Tests;

public class OrderConcurrencyTests
{
    [SqlServerFact]
    public async Task TenBuyersForThreePieces_ExactlyThreeOrdersSucceed()
    {
        await using var sql = new SqlServerTestDb();
        await sql.CreateAsync();

        int productId;
        await using (var setup = sql.NewContext())
        {
            await SeedData.ApplyAsync(setup);
            var product = await setup.Products.FirstAsync();
            product.Stock = 3;
            await setup.SaveChangesAsync();
            productId = product.Id;
        }

        var request = new CreateOrderRequest("Blerës Test", "044123456", "klient@mail.com", "Rruga 1, Prishtinë", "Prishtinë",
            PaymentMethod.CashOnDelivery, [new OrderItemRequest(productId, 1)]);

        var attempts = Enumerable.Range(0, 10).Select(async _ =>
        {
            await using var context = sql.NewContext();
            return await new PaymentTestKit(context).Orders.CreateOrder(request);
        });
        var results = await Task.WhenAll(attempts);

        Assert.Equal(3, results.Count(r => r.Failure is null));
        Assert.Equal(7, results.Count(r => r.Failure == OrderFailure.OutOfStock));

        await using var check = sql.NewContext();
        Assert.Equal(0, await check.Products.Where(p => p.Id == productId).Select(p => p.Stock).SingleAsync());
        Assert.Equal(3, await check.Orders.CountAsync());
        Assert.Equal(3, await check.OrderItems.SumAsync(i => i.Quantity));
    }
}
