using Dyndns.Service.Models;

namespace Dyndns.Service.Services;

public interface IIpChangeChecker
{
    Task<IpChangeCheckResult> CheckAsync(CancellationToken cancellationToken);
}