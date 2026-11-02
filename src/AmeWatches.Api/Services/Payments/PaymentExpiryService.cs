using AmeWatches.Api.Models.Payments;
using Microsoft.Extensions.Options;

namespace AmeWatches.Api.Services.Payments;

public class PaymentExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly PaymentOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<PaymentExpiryService> _logger;

    public PaymentExpiryService(IServiceScopeFactory scopes, IOptions<PaymentOptions> options, TimeProvider time, ILogger<PaymentExpiryService> logger)
    {
        _scopes = scopes;
        _options = options.Value;
        _time = time;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var payments = scope.ServiceProvider.GetRequiredService<IPaymentService>();
                var cutoff = _time.GetUtcNow().AddMinutes(-_options.ExpiryMinutes);
                var expired = await payments.ExpireUnpaidOrders(cutoff);
                if (expired > 0)
                    _logger.LogInformation("U anuluan {Count} porosi me kartë të papaguara.", expired);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Anulimi i porosive të papaguara dështoi.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
