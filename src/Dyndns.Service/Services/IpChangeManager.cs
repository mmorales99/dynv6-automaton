using Dyndns.Service.Models;
using Dyndns.Service.Options;
using Microsoft.Extensions.Options;

namespace Dyndns.Service.Services;

public sealed class IpChangeManager : IIpChangeManager
{
    private readonly IIpChangeChecker _ipChangeChecker;
    private readonly IDynv6Client _dynv6Client;
    private readonly ILogger<IpChangeManager> _logger;
    private readonly IOptions<Dynv6Options> _options;
    private readonly IUpdateStateStore _stateStore;

    public IpChangeManager(
        IIpChangeChecker ipChangeChecker,
        IDynv6Client dynv6Client,
        ILogger<IpChangeManager> logger,
        IOptions<Dynv6Options> options,
        IUpdateStateStore stateStore)
    {
        _ipChangeChecker = ipChangeChecker;
        _dynv6Client = dynv6Client;
        _logger = logger;
        _options = options;
        _stateStore = stateStore;
    }

    public Task<IpChangeCheckResult> CheckAsync(CancellationToken cancellationToken)
        => _ipChangeChecker.CheckAsync(cancellationToken);

    public async Task<UpdateCycleResult> UpdateIpAsync(string zoneName, string newIp, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(zoneName))
        {
            throw new ArgumentException("A zone name is required.", nameof(zoneName));
        }

        if (string.IsNullOrWhiteSpace(newIp))
        {
            throw new ArgumentException("A new IP address is required.", nameof(newIp));
        }

        var settings = _options.Value;
        var zone = await _dynv6Client.GetZoneByNameAsync(zoneName, cancellationToken);
        var records = await _dynv6Client.GetRecordsAsync(zone.Id, cancellationToken);
        var wildcardRecord = records.FirstOrDefault(record =>
            string.Equals(record.Name, settings.RecordName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(record.Type, "A", StringComparison.OrdinalIgnoreCase));

        if (wildcardRecord is null)
        {
            throw new InvalidOperationException($"Zone '{zoneName}' does not contain an A record named '{settings.RecordName}'.");
        }

        if (string.Equals(wildcardRecord.Data, newIp, StringComparison.Ordinal))
        {
            await _stateStore.SaveLastKnownIpAsync(newIp, cancellationToken);
            _logger.LogInformation("Dynv6 wildcard record already matches the current IPv4 address.");
            return new UpdateCycleResult(false, "Dynv6 record already matched the current IPv4 address.", newIp, wildcardRecord.Data);
        }

        var updatedRecord = await _dynv6Client.UpdateRecordAsync(
            zone.Id,
            wildcardRecord.Id,
            wildcardRecord.ToUpdateRequest(newIp),
            cancellationToken);

        await _stateStore.SaveLastKnownIpAsync(newIp, cancellationToken);

        _logger.LogInformation(
            "Updated Dynv6 record {RecordName} in zone {ZoneName} to {CurrentIpv4Address}.",
            updatedRecord.Name,
            zone.Name,
            newIp);

        return new UpdateCycleResult(true, $"Updated Dynv6 record '{updatedRecord.Name}' in zone '{zone.Name}'.", newIp, wildcardRecord.Data);
    }
}