using Dyndns.Service.Models;

namespace Dyndns.Service.Services;

public sealed class IpChangeManager : IIpChangeManager
{
    private const string WildcardRecordName = "*";

    private readonly IIpChangeChecker _ipChangeChecker;
    private readonly IDynv6Client _dynv6Client;
    private readonly ILogger<IpChangeManager> _logger;
    private readonly IUpdateStateStore _stateStore;

    public IpChangeManager(
        IIpChangeChecker ipChangeChecker,
        IDynv6Client dynv6Client,
        ILogger<IpChangeManager> logger,
        IUpdateStateStore stateStore)
    {
        _ipChangeChecker = ipChangeChecker;
        _dynv6Client = dynv6Client;
        _logger = logger;
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

        var zone = await _dynv6Client.GetZoneByNameAsync(zoneName, cancellationToken);
        var records = await _dynv6Client.GetRecordsAsync(zone.Id, cancellationToken);
        var wildcardRecord = records.FirstOrDefault(record =>
            string.Equals(record.Name, WildcardRecordName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(record.Type, "A", StringComparison.OrdinalIgnoreCase));

        if (wildcardRecord is null)
        {
            throw new InvalidOperationException($"Zone '{zoneName}' does not contain an A record named '{WildcardRecordName}'.");
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