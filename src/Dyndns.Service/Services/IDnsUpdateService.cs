using Dyndns.Service.Models;

namespace Dyndns.Service.Services;

public interface IDnsUpdateService
{
    Task<UpdateCycleResult> RunOnceAsync(CancellationToken cancellationToken);
}