using AmeWatches.Api.Data.Entities;

namespace AmeWatches.Api.Services.Email;

public interface IOrderNotifier
{
    void OrderConfirmed(Order order);
}

public class OrderNotifier : IOrderNotifier
{
    private readonly IEmailOutbox _outbox;
    private readonly OrderEmailBuilder _builder;
    private readonly ILogger<OrderNotifier> _logger;

    public OrderNotifier(IEmailOutbox outbox, OrderEmailBuilder builder, ILogger<OrderNotifier> logger)
    {
        _outbox = outbox;
        _builder = builder;
        _logger = logger;
    }

    public void OrderConfirmed(Order order)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(order.Email))
                _outbox.Enqueue(_builder.CustomerConfirmation(order));

            _outbox.Enqueue(_builder.ShopNotification(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Përgatitja e email-eve për porosinë {Order} dështoi; porosia mbetet e vlefshme.", order.OrderNo);
        }
    }
}
