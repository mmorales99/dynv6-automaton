using Dyndns.Service.Options;
using Dyndns.Service.Services;

namespace Dyndns.Service;

public sealed class DnsUpdateWorker : BackgroundService
{
    private readonly IUpdateCycleRunner _cycleRunner;
    private readonly IUpdateFailureNotifier _failureNotifier;
    private readonly IUpdateFailurePolicy _failurePolicy;
    private readonly ILogger<DnsUpdateWorker> _logger;
    private readonly IDynv6SettingsService _settingsService;

    public DnsUpdateWorker(
        IUpdateCycleRunner cycleRunner,
        IUpdateFailureNotifier failureNotifier,
        IUpdateFailurePolicy failurePolicy,
        ILogger<DnsUpdateWorker> logger,
        IDynv6SettingsService settingsService)
    {
        _cycleRunner = cycleRunner;
        _failureNotifier = failureNotifier;
        _failurePolicy = failurePolicy;
        _logger = logger;
        _settingsService = settingsService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nextDelay = TimeSpan.Zero;
        DateTimeOffset? lastFailureNotificationAt = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            if (nextDelay > TimeSpan.Zero)
            {
                await Task.Delay(nextDelay, stoppingToken);
            }

            var now = DateTimeOffset.Now;
            var result = await _cycleRunner.RunAsync("scheduled", stoppingToken);

            if (result.Succeeded)
            {
                _logger.LogInformation("{CycleMessage}", result.Message);
                var settings = await _settingsService.GetAsync(stoppingToken);
                nextDelay = settings.ScheduledEvery;
                lastFailureNotificationAt = null;
                continue;
            }

            _logger.LogError("{CycleMessage}", result.Error ?? result.Message);

            if (_failurePolicy.ShouldNotify(now, lastFailureNotificationAt))
            {
                await SendFailureNotificationAsync(result.Error ?? result.Message, now, stoppingToken);
                lastFailureNotificationAt = now;
            }

            nextDelay = _failurePolicy.GetRetryDelay();
        }
    }

    private async Task SendFailureNotificationAsync(string errorDetails, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var zoneName = (await _settingsService.GetAsync(cancellationToken)).ZoneName;
        var subject = string.IsNullOrWhiteSpace(zoneName)
            ? "Update IP failed"
            : $"Update IP failed for {zoneName}";

        var body = $"Update IP procedure failed at {now:O}.\n\n{errorDetails}";

        try
        {
            await _failureNotifier.SendAsync(subject, body, cancellationToken);
        }
        catch (Exception notificationException)
        {
            _logger.LogError(notificationException, "Dynv6 failure notification could not be sent.");
        }
    }
}
