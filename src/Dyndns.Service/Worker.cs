using Dyndns.Service.Options;
using Dyndns.Service.Services;
using Microsoft.Extensions.Options;

namespace Dyndns.Service;

public sealed class DnsUpdateWorker : BackgroundService
{
    private readonly IDnsUpdateService _dnsUpdateService;
    private readonly IUpdateFailureNotifier _failureNotifier;
    private readonly IUpdateFailurePolicy _failurePolicy;
    private readonly ILogger<DnsUpdateWorker> _logger;
    private readonly IOptions<Dynv6Options> _options;

    public DnsUpdateWorker(
        IDnsUpdateService dnsUpdateService,
        IUpdateFailureNotifier failureNotifier,
        IUpdateFailurePolicy failurePolicy,
        ILogger<DnsUpdateWorker> logger,
        IOptions<Dynv6Options> options)
    {
        _dnsUpdateService = dnsUpdateService;
        _failureNotifier = failureNotifier;
        _failurePolicy = failurePolicy;
        _logger = logger;
        _options = options;
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

            try
            {
                var result = await _dnsUpdateService.RunOnceAsync(stoppingToken);
                _logger.LogInformation("{CycleMessage}", result.Message);
                nextDelay = _failurePolicy.GetHealthyDelay();
                lastFailureNotificationAt = null;
            }
            catch (Exception exception)
            {
                var now = DateTimeOffset.Now;
                _logger.LogError(exception, "Dynv6 update cycle failed.");

                if (_failurePolicy.ShouldNotify(now, lastFailureNotificationAt))
                {
                    await SendFailureNotificationAsync(exception, now, stoppingToken);
                    lastFailureNotificationAt = now;
                }

                nextDelay = _failurePolicy.GetRetryDelay();
            }
        }
    }

    private async Task SendFailureNotificationAsync(Exception exception, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var zoneName = _options.Value.ZoneName;
        var subject = string.IsNullOrWhiteSpace(zoneName)
            ? "Update IP failed"
            : $"Update IP failed for {zoneName}";

        var body = $"Update IP procedure failed at {now:O}.\n\n{exception}";

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
