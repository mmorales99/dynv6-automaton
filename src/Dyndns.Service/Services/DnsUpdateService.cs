using Dyndns.Service.Domain;
using Dyndns.Service.Models;
using Dyndns.Service.Options;

namespace Dyndns.Service.Services;

public sealed class DnsUpdateService : IDnsUpdateService
{
    private readonly IIpChangeManager _ipChangeManager;
    private readonly ILogger<DnsUpdateService> _logger;
    private readonly IDynv6SettingsService _settingsService;

    public DnsUpdateService(
        IIpChangeManager ipChangeManager,
        ILogger<DnsUpdateService> logger,
        IDynv6SettingsService settingsService)
    {
        _ipChangeManager = ipChangeManager;
        _logger = logger;
        _settingsService = settingsService;
    }

    public async Task<UpdateCycleResult> RunOnceAsync(CancellationToken cancellationToken, bool forceUpdate = false)
    {
        var settings = await _settingsService.GetAsync(cancellationToken);
        ValidateSettings(settings);

        var changeCheck = await _ipChangeManager.CheckAsync(cancellationToken);

        var shouldForceUpdate = settings.ForceUpdate || forceUpdate;

        if (!changeCheck.HasChanged && !shouldForceUpdate)
        {
            _logger.LogInformation("Skipping Dynv6 update because the IPv4 address has not changed.");
            return new UpdateCycleResult(false, "IPv4 address unchanged.", changeCheck.CurrentIp, changeCheck.PreviousIp);
        }

        return await _ipChangeManager.UpdateIpAsync(settings.ZoneName, changeCheck.CurrentIp, cancellationToken);
    }

    private static void ValidateSettings(Dynv6Options settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ZoneName))
        {
            throw new InvalidOperationException("Dynv6:ZoneName is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.Key))
        {
            throw new InvalidOperationException("Dynv6:Key is required.");
        }
    }
}