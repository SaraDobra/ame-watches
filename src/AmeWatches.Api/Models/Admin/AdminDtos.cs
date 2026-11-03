using System.ComponentModel.DataAnnotations;
using AmeWatches.Api.Data.Entities;

namespace AmeWatches.Api.Models.Admin;

public record ProductInput(
    [Required, StringLength(200, MinimumLength = 2)] string Name,
    [Required, StringLength(50, MinimumLength = 2)] string ModelCode,
    [Range(1, int.MaxValue)] int BrandId,
    [StringLength(2000)] string? Description,
    [Range(0.01, 100000)] decimal Price,
    [Range(0, 100000)] int Stock,
    [EnumDataType(typeof(WatchGender))] WatchGender Gender,
    [EnumDataType(typeof(WatchType))] WatchType Type,
    [Range(10, 100)] int? CaseSizeMm,
    [Range(0, 1000)] int? WaterResistanceM,
    [StringLength(50)] string? Movement,
    bool IsActive
);

public record SetActiveRequest(bool IsActive);

public record ChangeStatusRequest([EnumDataType(typeof(OrderStatus))] OrderStatus Status);

public record ReorderImagesRequest([Required, MinLength(1)] List<int> ImageIds);

public class AdminProductQuery
{
    [StringLength(100)]
    public string? Search { get; set; }

    public bool? IsActive { get; set; }

    [Range(1, 10000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public class AdminOrderQuery
{
    public OrderStatus? Status { get; set; }

    [StringLength(100)]
    public string? Search { get; set; }

    [Range(1, 10000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public record AdminImage(int Id, string Url, int SortOrder);

public record AdminProductListItem(
    int Id, string Name, string Slug, string ModelCode, string Brand,
    decimal Price, int Stock, bool IsActive, string? ImageUrl);

public record AdminProductDetail(
    int Id, string Name, string Slug, string ModelCode, int BrandId, string Brand,
    string? Description, decimal Price, int Stock,
    string Gender, string Type, int? CaseSizeMm, int? WaterResistanceM, string? Movement,
    bool IsActive, IReadOnlyList<AdminImage> Images);

public record AdminOrderListItem(
    string OrderNo, string CustomerName, string Phone, decimal Total,
    string Status, string PaymentMethod, string PaymentStatus, DateTime CreatedAt, int ItemCount);

public record AdminOrderDetail(
    string OrderNo, string Status, string PaymentMethod, string PaymentStatus,
    string CustomerName, string Phone, string? Email, string Address, string City,
    decimal Subtotal, decimal Shipping, decimal Total,
    DateTime CreatedAt, DateTime? PaidAt, DateTime? WarrantyUntil,
    bool NeedsRefund, IReadOnlyList<string> AllowedStatuses,
    IReadOnlyList<AdminOrderLine> Items);

public record AdminOrderLine(int ProductId, string ProductName, string ModelCode, decimal UnitPrice, int Quantity, decimal LineTotal);

public record LowStockItem(int Id, string Name, string ModelCode, int Stock);

public record DashboardSummary(
    int NewOrders, int AwaitingPayment, int ToShip, int OrdersToday,
    decimal RevenueThisMonth, IReadOnlyList<LowStockItem> LowStock);

public record PagedAdmin<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public enum AdminError
{
    NotFound = 1,
    Conflict = 2,
    Invalid = 3
}

public record AdminResult<T>(T? Value, AdminError? Error, string? Message)
{
    public bool Succeeded => Error is null;
    public static AdminResult<T> Ok(T value) => new(value, null, null);
    public static AdminResult<T> NotFound(string message = "Nuk u gjet.") => new(default, AdminError.NotFound, message);
    public static AdminResult<T> Conflict(string message) => new(default, AdminError.Conflict, message);
    public static AdminResult<T> Invalid(string message) => new(default, AdminError.Invalid, message);
}
