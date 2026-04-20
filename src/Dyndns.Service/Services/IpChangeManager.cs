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

        _logger.LogInformation("Looking up Dynv6 zone {ZoneName}.", zoneName);
        var zone = await _dynv6Client.GetZoneByNameAsync(zoneName, cancellationToken);
        _logger.LogDebug("Found Dynv6 zone {ZoneName} with id {ZoneId}.", zone.Name, zone.Id);

        var records = await _dynv6Client.GetRecordsAsync(zone.Id, cancellationToken);
        var wildcardRecord = records.FirstOrDefault(record =>
            string.Equals(record.Name, WildcardRecordName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(record.Type, "A", StringComparison.OrdinalIgnoreCase));

        if (wildcardRecord is null)
        {
            _logger.LogWarning("Dynv6 zone {ZoneName} does not contain an A record named {RecordName}.", zoneName, WildcardRecordName);
            throw new InvalidOperationException($"Zone '{zoneName}' does not contain an A record named '{WildcardRecordName}'.");
        }

        if (string.Equals(wildcardRecord.Data, newIp, StringComparison.Ordinal))
        {
            _logger.LogDebug(
                "Dynv6 wildcard record for zone {ZoneName} already matches the current IPv4 address {CurrentIp}.",
                zoneName,
                newIp);
            await _stateStore.SaveLastKnownIpAsync(newIp, cancellationToken);
            return new UpdateCycleResult(false, "Dynv6 record already matched the current IPv4 address.", newIp, wildcardRecord.Data);
        }

        _logger.LogInformation(
            "Updating Dynv6 wildcard record for zone {ZoneName} from {PreviousIp} to {CurrentIp}.",
            zoneName,
            wildcardRecord.Data,
            newIp);
        var updatedRecord = await _dynv6Client.UpdateRecordAsync(
            zone.Id,
            wildcardRecord.Id,
            wildcardRecord.ToUpdateRequest(newIp),
            cancellationToken);

        await _dynv6Client.UpdateZoneAsync(
            zone.Id,
            new Dynv6ZoneUpdateRequest
            {
                Ipv4Address = newIp
            },
            cancellationToken);

        await _stateStore.SaveLastKnownIpAsync(newIp, cancellationToken);

        _logger.LogDebug(
            "Updated Dynv6 record {RecordName} in zone {ZoneName} to {CurrentIpv4Address}.",
            updatedRecord.Name,
            zone.Name,
            newIp);

        return new UpdateCycleResult(true, $"Updated Dynv6 record '{updatedRecord.Name}' in zone '{zone.Name}'.", newIp, wildcardRecord.Data);
    }
}