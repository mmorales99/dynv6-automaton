using Dyndns.Service.Models;

namespace Dyndns.Service.Services;

public interface IUpdateCycleRunner
{
    Task<UpdateRunEntry> RunAsync(string trigger, CancellationToken cancellationToken, bool forceUpdate = false);

    Task<IReadOnlyList<UpdateRunEntry>> ReadRecentRunsAsync(int maxEntries, CancellationToken cancellationToken);
}