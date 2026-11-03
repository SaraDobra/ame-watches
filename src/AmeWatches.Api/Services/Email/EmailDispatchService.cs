namespace AmeWatches.Api.Services.Email;

public class EmailDispatchService : BackgroundService
{
    private readonly EmailOutbox _outbox;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<EmailDispatchService> _logger;

    public TimeSpan[] Delays { get; init; } = [TimeSpan.Zero, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30)];

    public EmailDispatchService(EmailOutbox outbox, IServiceScopeFactory scopes, ILogger<EmailDispatchService> logger)
    {
        _outbox = outbox;
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _outbox.Reader.ReadAllAsync(stoppingToken))
        {
            for (var attempt = 0; attempt < Delays.Length; attempt++)
            {
                try
                {
                    if (Delays[attempt] > TimeSpan.Zero)
                        await Task.Delay(Delays[attempt], stoppingToken);

                    using var scope = _scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<IEmailSender>().Send(message, stoppingToken);
                    break;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Dërgimi i email-it te {To} dështoi (përpjekja {Attempt} nga {Total}).", message.To, attempt + 1, Delays.Length);
                    if (attempt == Delays.Length - 1)
                        _logger.LogError("Email-i \"{Subject}\" te {To} nuk u dërgua pas {Total} përpjekjesh.", message.Subject, message.To, Delays.Length);
                }
            }
        }
    }
}
