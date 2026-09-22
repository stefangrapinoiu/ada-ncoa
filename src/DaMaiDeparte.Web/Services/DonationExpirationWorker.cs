namespace DaMaiDeparte.Web.Services;

/// <summary>
/// Moves listings whose expiry date has passed into the Expired status.
///
/// The feed already excludes expired food at query level, so this worker is not what keeps
/// unsafe listings out of the dashboard — it is what keeps the stored status honest, so a
/// donor sees "Expirat" on their own item and so the data stays usable for reporting.
/// </summary>
public sealed class DonationExpirationWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DonationExpirationWorker> _logger;

    public DonationExpirationWorker(IServiceScopeFactory scopeFactory, ILogger<DonationExpirationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await SafeWaitAsync(timer, stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var donations = scope.ServiceProvider.GetRequiredService<IDonationService>();
            await donations.ExpireDueDonationsAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            // A failed sweep must never take the application down; the next tick retries.
            _logger.LogError(ex, "Expiration sweep failed");
        }
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
