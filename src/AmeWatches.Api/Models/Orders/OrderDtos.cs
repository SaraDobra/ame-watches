using System.ComponentModel.DataAnnotations;
using AmeWatches.Api.Data.Entities;

namespace AmeWatches.Api.Models.Orders;

public record OrderItemRequest(
    [Range(1, int.MaxValue)] int ProductId,
    [Range(1, 10)] int Quantity
);

public record CreateOrderRequest(
    [Required, StringLength(150, MinimumLength = 3)] string CustomerName,
    [Required, RegularExpression(@"^\+?[0-9][0-9\s\-]{6,18}[0-9]$", ErrorMessage = "Numri i telefonit nuk është i vlefshëm.")] string Phone,
    [Required, EmailAddress, StringLength(150)] string Email,
    [Required, StringLength(250, MinimumLength = 5)] string Address,
    [Required, StringLength(100, MinimumLength = 2)] string City,
    [EnumDataType(typeof(PaymentMethod))] PaymentMethod PaymentMethod,
    [Required, MinLength(1), MaxLength(20)] List<OrderItemRequest> Items
);

public record OrderCreatedResponse(string OrderNo, string AccessToken, decimal Total, string? PaymentUrl = null);

public record OrderLineResponse(
    int ProductId,
    string ProductName,
    string ModelCode,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal
);

public record OrderResponse(
    string OrderNo,
    string Status,
    string PaymentMethod,
    string PaymentStatus,
    string CustomerName,
    string Phone,
    string? Email,
    string Address,
    string City,
    decimal Subtotal,
    decimal Shipping,
    decimal Total,
    DateTime CreatedAt,
    DateTime? PaidAt,
    IReadOnlyList<OrderLineResponse> Items
);

public enum OrderFailure
{
    InvalidItems = 1,
    OutOfStock = 2,
    PaymentUnavailable = 3
}

public record CreateOrderResult(OrderCreatedResponse? Created, OrderFailure? Failure, string? Message)
{
    public static CreateOrderResult Ok(OrderCreatedResponse created) => new(created, null, null);
    public static CreateOrderResult Fail(OrderFailure failure, string message) => new(null, failure, message);
}
