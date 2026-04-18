using Dyndns.Service.Domain;
using Dyndns.Service.Models;
using Dyndns.Service.Options;
using Microsoft.Extensions.Options;

namespace Dyndns.Service.Services;

public sealed class DnsUpdateService : IDnsUpdateService
{
    private readonly IIpChangeManager _ipChangeManager;
    private readonly ILogger<DnsUpdateService> _logger;
    private readonly IOptions<Dynv6Options> _options;

    public DnsUpdateService(
        IIpChangeManager ipChangeManager,
        ILogger<DnsUpdateService> logger,
        IOptions<Dynv6Options> options)
    {
        _ipChangeManager = ipChangeManager;
        _logger = logger;
        _options = options;
    }

    public async Task<UpdateCycleResult> RunOnceAsync(CancellationToken cancellationToken)
    {
        var settings = _options.Value;
        ValidateSettings(settings);

        var changeCheck = await _ipChangeManager.CheckAsync(cancellationToken);

        if (!changeCheck.HasChanged && !settings.ForceUpdate)
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