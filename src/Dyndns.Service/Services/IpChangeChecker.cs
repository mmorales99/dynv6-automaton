using Dyndns.Service.Domain;
using Dyndns.Service.Models;

namespace Dyndns.Service.Services;

public sealed class IpChangeChecker : IIpChangeChecker
{
    private readonly IPublicIpProvider _publicIpProvider;
    private readonly IUpdateStateStore _stateStore;

    public IpChangeChecker(IPublicIpProvider publicIpProvider, IUpdateStateStore stateStore)
    {
        _publicIpProvider = publicIpProvider;
        _stateStore = stateStore;
    }

    public async Task<IpChangeCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        var currentIp = await _publicIpProvider.GetCurrentIpv4AddressAsync(cancellationToken);
        var previousIp = await _stateStore.ReadLastKnownIpAsync(cancellationToken);
        var hasChanged = UpdateDecision.ShouldUpdate(previousIp, currentIp, forceUpdate: false);

        return new IpChangeCheckResult(hasChanged, currentIp, previousIp);
    }
}