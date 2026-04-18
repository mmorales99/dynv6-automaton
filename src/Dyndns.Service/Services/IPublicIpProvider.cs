namespace Dyndns.Service.Services;

public interface IPublicIpProvider
{
    Task<string> GetCurrentIpv4AddressAsync(CancellationToken cancellationToken);
}