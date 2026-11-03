using AmeWatches.Api.Data;
using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Admin;
using AmeWatches.Api.Services.Payments;
using Microsoft.EntityFrameworkCore;

namespace AmeWatches.Api.Services.Admin;

public interface IAdminOrderService
{
    Task<PagedAdmin<AdminOrderListItem>> List(AdminOrderQuery query);
    Task<AdminOrderDetail?> Get(string orderNo);
    Task<AdminResult<AdminOrderDetail>> ChangeStatus(string orderNo, OrderStatus target);
    Task<DashboardSummary> GetSummary();
}

public class AdminOrderService : IAdminOrderService
{
    public const int LowStockThreshold = 2;

    private static readonly Dictionary<OrderStatus, OrderStatus[]> Transitions = new()
    {
        [OrderStatus.New] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Delivered, OrderStatus.Cancelled],
        [OrderStatus.Delivered] = [],
        [OrderStatus.Cancelled] = []
    };

    private readonly AppDbContext _db;
    private readonly IPaymentService _payments;
    private readonly TimeProvider _time;

    public AdminOrderService(AppDbContext db, IPaymentService payments, TimeProvider time)
    {
        _db = db;
        _payments = payments;
        _time = time;
    }

    public async Task<PagedAdmin<AdminOrderListItem>> List(AdminOrderQuery query)
    {
        var orders = _db.Orders.AsNoTracking().AsQueryable();

        if (query.Status is OrderStatus status)
            orders = orders.Where(o => o.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLike(query.Search.Trim())}%";
            orders = orders.Where(o =>
                EF.Functions.Like(o.OrderNo, pattern, "\\") ||
                EF.Functions.Like(o.CustomerName, pattern, "\\") ||
                EF.Functions.Like(o.Phone, pattern, "\\"));
        }

        var total = await orders.CountAsync();

        var items = await orders
            .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(o => new AdminOrderListItem(
                o.OrderNo, o.CustomerName, o.Phone, o.Total,
                o.Status.ToString(), o.PaymentMethod.ToString(), o.PaymentStatus.ToString(),
                o.CreatedAt, o.Items.Count))
            .ToListAsync();

        return new PagedAdmin<AdminOrderListItem>(items, query.Page, query.PageSize, total);
    }

    public async Task<AdminOrderDetail?> Get(string orderNo)
    {
        var order = await _db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.OrderNo == orderNo);
        return order is null ? null : ToDetail(order);
    }

    public async Task<AdminResult<AdminOrderDetail>> ChangeStatus(string orderNo, OrderStatus target)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.OrderNo == orderNo);
        if (order is null)
            return AdminResult<AdminOrderDetail>.NotFound("Porosia nuk u gjet.");

        if (!Transitions[order.Status].Contains(target))
            return AdminResult<AdminOrderDetail>.Conflict($"Nuk mund të kalohet nga \"{order.Status}\" në \"{target}\".");

        if (order.PaymentMethod == PaymentMethod.Card && order.PaymentStatus != PaymentStatus.Paid && target != OrderStatus.Cancelled)
            return AdminResult<AdminOrderDetail>.Conflict("Porosia me kartë nuk është paguar ende.");

        var now = _time.GetUtcNow().UtcDateTime;

        if (target == OrderStatus.Cancelled)
        {
            await _payments.CancelAndRestock(order.Id);
        }
        else
        {
            order.Status = target;

            if (target == OrderStatus.Delivered)
            {
                order.WarrantyUntil = now.AddYears(1);
                if (order.PaymentMethod == PaymentMethod.CashOnDelivery)
                {
                    order.PaymentStatus = PaymentStatus.Paid;
                    order.PaidAt = now;
                }
            }

            await _db.SaveChangesAsync();
        }

        return AdminResult<AdminOrderDetail>.Ok((await Get(orderNo))!);
    }

    public async Task<DashboardSummary> GetSummary()
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var today = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var orders = _db.Orders.AsNoTracking();

        var newOrders = await orders.CountAsync(o => o.Status == OrderStatus.New && o.PaymentMethod == PaymentMethod.CashOnDelivery);
        var awaiting = await orders.CountAsync(o => o.Status == OrderStatus.New && o.PaymentMethod == PaymentMethod.Card && o.PaymentStatus == PaymentStatus.Pending);
        var toShip = await orders.CountAsync(o => o.Status == OrderStatus.Confirmed);
        var todayCount = await orders.CountAsync(o => o.CreatedAt >= today);

        var revenue = (await orders
            .Where(o => o.CreatedAt >= monthStart && o.Status != OrderStatus.Cancelled && o.PaymentStatus == PaymentStatus.Paid)
            .Select(o => o.Total)
            .ToListAsync()).Sum();

        var lowStock = await _db.Products.AsNoTracking()
            .Where(p => p.IsActive && p.Stock <= LowStockThreshold)
            .OrderBy(p => p.Stock).ThenBy(p => p.Name)
            .Select(p => new LowStockItem(p.Id, p.Name, p.ModelCode, p.Stock))
            .ToListAsync();

        return new DashboardSummary(newOrders, awaiting, toShip, todayCount, revenue, lowStock);
    }

    private static AdminOrderDetail ToDetail(Order o) => new(
        o.OrderNo, o.Status.ToString(), o.PaymentMethod.ToString(), o.PaymentStatus.ToString(),
        o.CustomerName, o.Phone, o.Email, o.Address, o.City,
        o.Subtotal, o.Shipping, o.Total, o.CreatedAt, o.PaidAt, o.WarrantyUntil,
        o.Status == OrderStatus.Cancelled && o.PaymentStatus == PaymentStatus.Paid,
        Transitions[o.Status].Select(s => s.ToString()).ToList(),
        o.Items.Select(i => new AdminOrderLine(i.ProductId, i.ProductName, i.ModelCode, i.UnitPrice, i.Quantity, i.UnitPrice * i.Quantity)).ToList());

    private static string EscapeLike(string text)
        => text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
}
