namespace Dyndns.Service.Services;

public interface IUpdateStateStore
{
    Task<string?> ReadLastKnownIpAsync(CancellationToken cancellationToken);

    Task SaveLastKnownIpAsync(string ipAddress, CancellationToken cancellationToken);
}