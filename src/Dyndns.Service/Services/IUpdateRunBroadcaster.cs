using Dyndns.Service.Models;

namespace Dyndns.Service.Services;

public interface IUpdateRunBroadcaster
{
    IAsyncEnumerable<UpdateRunEntry> SubscribeAsync(CancellationToken cancellationToken);

    void Publish(UpdateRunEntry entry);
}