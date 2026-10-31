using System.Security.Cryptography;
using System.Text;
using AmeWatches.Api.Data;
using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Orders;
using Microsoft.EntityFrameworkCore;

namespace AmeWatches.Api.Services;

public class OrderService : IOrderService
{
    public const decimal ShippingCost = 0m;
    public const int MaxQuantityPerProduct = 10;

    private const string OrderNoAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly AppDbContext _db;
    public OrderService(AppDbContext db) => _db = db;

    public async Task<CreateOrderResult> CreateOrder(CreateOrderRequest request)
    {
        if (request.PaymentMethod != PaymentMethod.CashOnDelivery)
            return CreateOrderResult.Fail(OrderFailure.PaymentUnavailable, "Pagesa me kartë nuk është ende e disponueshme. Zgjidhni pagesën në dorëzim.");

        var lines = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => (ProductId: g.Key, Quantity: g.Sum(i => i.Quantity)))
            .ToList();

        if (lines.Any(l => l.Quantity > MaxQuantityPerProduct))
            return CreateOrderResult.Fail(OrderFailure.InvalidItems, $"Mund të porositni deri në {MaxQuantityPerProduct} copë për çdo orë.");

        var ids = lines.Select(l => l.ProductId).ToList();
        var products = await _db.Products.AsNoTracking()
            .Include(p => p.Brand)
            .Where(p => ids.Contains(p.Id) && p.IsActive)
            .ToDictionaryAsync(p => p.Id);

        if (products.Count != ids.Count)
            return CreateOrderResult.Fail(OrderFailure.InvalidItems, "Një nga orët në shportë nuk është më e disponueshme. Hiqeni nga shporta dhe provoni përsëri.");

        await using var transaction = await _db.Database.BeginTransactionAsync();

        foreach (var (productId, quantity) in lines)
        {
            var updated = await _db.Products
                .Where(p => p.Id == productId && p.IsActive && p.Stock >= quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - quantity));

            if (updated == 0)
            {
                await transaction.RollbackAsync();
                return CreateOrderResult.Fail(OrderFailure.OutOfStock, $"Na vjen keq, \"{products[productId].Brand.Name} {products[productId].Name}\" nuk ka stok të mjaftueshëm.");
            }
        }

        var items = lines.Select(l =>
        {
            var product = products[l.ProductId];
            return new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ModelCode = product.ModelCode,
                UnitPrice = product.Price,
                Quantity = l.Quantity
            };
        }).ToList();

        var subtotal = items.Sum(i => i.UnitPrice * i.Quantity);

        var order = new Order
        {
            OrderNo = await GenerateOrderNo(),
            AccessToken = GenerateToken(),
            Status = OrderStatus.New,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = PaymentStatus.Pending,
            CustomerName = request.CustomerName.Trim(),
            Phone = request.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            Subtotal = subtotal,
            Shipping = ShippingCost,
            Total = subtotal + ShippingCost,
            CreatedAt = DateTime.UtcNow,
            Items = items
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return CreateOrderResult.Ok(new OrderCreatedResponse(order.OrderNo, order.AccessToken, order.Total));
    }

    public async Task<OrderResponse?> GetOrder(string orderNo, string token)
    {
        if (string.IsNullOrWhiteSpace(orderNo) || string.IsNullOrWhiteSpace(token))
            return null;

        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNo == orderNo);

        if (order is null || !TokensMatch(order.AccessToken, token))
            return null;

        return new OrderResponse(
            order.OrderNo,
            order.Status.ToString(),
            order.PaymentMethod.ToString(),
            order.PaymentStatus.ToString(),
            order.CustomerName,
            order.Phone,
            order.Email,
            order.Address,
            order.City,
            order.Subtotal,
            order.Shipping,
            order.Total,
            order.CreatedAt,
            order.Items.Select(i => new OrderLineResponse(i.ProductId, i.ProductName, i.ModelCode, i.UnitPrice, i.Quantity, i.UnitPrice * i.Quantity)).ToList());
    }

    private async Task<string> GenerateOrderNo()
    {
        while (true)
        {
            var chars = RandomNumberGenerator.GetItems<char>(OrderNoAlphabet, 6);
            var orderNo = $"AW-{new string(chars)}";
            if (!await _db.Orders.AnyAsync(o => o.OrderNo == orderNo))
                return orderNo;
        }
    }

    private static string GenerateToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static bool TokensMatch(string expected, string actual)
        => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(actual));
}
