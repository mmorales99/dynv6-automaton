using Dyndns.Service.Models;

namespace Dyndns.Service.Services;

public interface IUpdateRunHistoryStore
{
    Task AppendAsync(UpdateRunEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyList<UpdateRunEntry>> ReadRecentAsync(int maxEntries, CancellationToken cancellationToken);
}