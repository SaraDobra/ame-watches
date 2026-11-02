using AmeWatches.Api.Data;
using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AmeWatches.Api.Services.Payments;

public class PaymentService : IPaymentService
{
    private const string Currency = "EUR";

    private readonly AppDbContext _db;
    private readonly IPaymentProvider _provider;
    private readonly PaymentOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(AppDbContext db, IPaymentProvider provider, IOptions<PaymentOptions> options, TimeProvider time, ILogger<PaymentService> logger)
    {
        _db = db;
        _provider = provider;
        _options = options.Value;
        _time = time;
        _logger = logger;
    }

    public async Task<PaymentSession> StartCardPayment(Order order)
    {
        var session = await _provider.CreateSession(new PaymentRequest(
            order.OrderNo, order.AccessToken, order.Total, Currency, $"amé watches {order.OrderNo}"));

        order.PaymentSessionId = session.SessionId;
        await _db.SaveChangesAsync();
        return session;
    }

    public async Task<WebhookResult> HandleWebhook(string body, string? signature)
    {
        var paymentEvent = _provider.ParseWebhook(body, signature);
        if (paymentEvent is null)
            return WebhookResult.Invalid;

        var now = _time.GetUtcNow();
        if (Math.Abs((now - paymentEvent.OccurredAt).TotalSeconds) > _options.WebhookToleranceSeconds)
        {
            _logger.LogWarning("Webhook i refuzuar: koha e ngjarjes është jashtë tolerancës ({Session}).", paymentEvent.SessionId);
            return WebhookResult.Invalid;
        }

        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.PaymentSessionId == paymentEvent.SessionId);
        if (order is null)
            return WebhookResult.NotFound;

        if (paymentEvent.Amount != order.Total || !string.Equals(paymentEvent.Currency, Currency, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Webhook i injoruar: shuma {Amount} {Currency} nuk përputhet me porosinë {Order} ({Total}).",
                paymentEvent.Amount, paymentEvent.Currency, order.OrderNo, order.Total);
            return WebhookResult.Ignored;
        }

        return paymentEvent.Outcome == PaymentOutcome.Paid
            ? await ApplyPaid(order, now)
            : await ApplyFailed(order);
    }

    public async Task<PaymentSessionInfo?> GetSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return null;

        return await _db.Orders.AsNoTracking()
            .Where(o => o.PaymentSessionId == sessionId)
            .Select(o => new PaymentSessionInfo(o.OrderNo, o.Total, Currency, o.PaymentStatus.ToString()))
            .FirstOrDefaultAsync();
    }

    public async Task CancelAndRestock(int orderId)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null || order.Status == OrderStatus.Cancelled)
            return;

        await using var transaction = await _db.Database.BeginTransactionAsync();
        await Restock(order);
        order.Status = OrderStatus.Cancelled;
        if (order.PaymentStatus == PaymentStatus.Pending && order.PaymentMethod == PaymentMethod.Card)
            order.PaymentStatus = PaymentStatus.Failed;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<int> ExpireUnpaidOrders(DateTimeOffset cutoff)
    {
        var cutoffUtc = cutoff.UtcDateTime;
        var ids = await _db.Orders.AsNoTracking()
            .Where(o => o.PaymentMethod == PaymentMethod.Card
                        && o.PaymentStatus == PaymentStatus.Pending
                        && o.Status == OrderStatus.New
                        && o.CreatedAt < cutoffUtc)
            .Select(o => o.Id)
            .ToListAsync();

        foreach (var id in ids)
            await CancelAndRestock(id);

        return ids.Count;
    }

    private async Task<WebhookResult> ApplyPaid(Order order, DateTimeOffset now)
    {
        if (order.PaymentStatus == PaymentStatus.Paid)
            return WebhookResult.Accepted;

        if (order.Status != OrderStatus.Cancelled)
        {
            order.PaymentStatus = PaymentStatus.Paid;
            order.PaidAt = now.UtcDateTime;
            order.Status = OrderStatus.Confirmed;
            await _db.SaveChangesAsync();
            return WebhookResult.Accepted;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var reserved = true;
        foreach (var item in order.Items)
        {
            var updated = await _db.Products
                .Where(p => p.Id == item.ProductId && p.Stock >= item.Quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - item.Quantity));
            if (updated == 0)
            {
                reserved = false;
                break;
            }
        }

        if (!reserved)
        {
            await transaction.RollbackAsync();
            order.PaymentStatus = PaymentStatus.Paid;
            order.PaidAt = now.UtcDateTime;
            await _db.SaveChangesAsync();
            _logger.LogError("Porosia {Order} u pagua pasi u anulua dhe stoku nuk mjafton. Duhet rimbursim.", order.OrderNo);
            return WebhookResult.Accepted;
        }

        order.Status = OrderStatus.Confirmed;
        order.PaymentStatus = PaymentStatus.Paid;
        order.PaidAt = now.UtcDateTime;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return WebhookResult.Accepted;
    }

    private async Task<WebhookResult> ApplyFailed(Order order)
    {
        if (order.PaymentStatus == PaymentStatus.Paid)
            return WebhookResult.Ignored;

        await CancelAndRestock(order.Id);
        return WebhookResult.Accepted;
    }

    private async Task Restock(Order order)
    {
        foreach (var item in order.Items)
        {
            await _db.Products
                .Where(p => p.Id == item.ProductId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + item.Quantity));
        }
    }
}
