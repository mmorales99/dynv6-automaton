namespace Dyndns.Service.Services;

public sealed class IpifyPublicIpProvider : IPublicIpProvider
{
    private readonly IpifyClient _ipifyClient;

    public IpifyPublicIpProvider(IpifyClient ipifyClient)
    {
        _ipifyClient = ipifyClient;
    }

    public Task<string> GetCurrentIpv4AddressAsync(CancellationToken cancellationToken)
        => _ipifyClient.GetCurrentIpv4AddressAsync(cancellationToken);
}