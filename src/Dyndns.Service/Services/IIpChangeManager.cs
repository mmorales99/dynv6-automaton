using Dyndns.Service.Models;

namespace Dyndns.Service.Services;

public interface IIpChangeManager
{
    Task<IpChangeCheckResult> CheckAsync(CancellationToken cancellationToken);

    Task<UpdateCycleResult> UpdateIpAsync(string zoneName, string newIp, CancellationToken cancellationToken);
}