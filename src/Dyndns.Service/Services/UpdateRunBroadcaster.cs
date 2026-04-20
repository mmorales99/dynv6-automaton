using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Runtime.CompilerServices;
using Dyndns.Service.Models;

namespace Dyndns.Service.Services;

public sealed class UpdateRunBroadcaster : IUpdateRunBroadcaster
{
    private readonly ConcurrentDictionary<int, Channel<UpdateRunEntry>> _subscribers = new();
    private int _nextSubscriberId;

    public IAsyncEnumerable<UpdateRunEntry> SubscribeAsync(CancellationToken cancellationToken)
    {
        var subscriberId = Interlocked.Increment(ref _nextSubscriberId);
        var channel = Channel.CreateUnbounded<UpdateRunEntry>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = true
        });

        _subscribers[subscriberId] = channel;

        return ReadAsync(subscriberId, channel.Reader, cancellationToken);
    }

    public void Publish(UpdateRunEntry entry)
    {
        var failedSubscriberIds = _subscribers
            .Where(subscriber => !subscriber.Value.Writer.TryWrite(entry))
            .Select(subscriber => subscriber.Key)
            .ToArray();

        foreach (var subscriberId in failedSubscriberIds)
        {
            RemoveSubscriber(subscriberId, null);
        }
    }

    private async IAsyncEnumerable<UpdateRunEntry> ReadAsync(
        int subscriberId,
        ChannelReader<UpdateRunEntry> reader,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var entry in reader.ReadAllAsync(cancellationToken))
            {
                yield return entry;
            }
        }
        finally
        {
            RemoveSubscriber(subscriberId, null);
        }
    }

    private void RemoveSubscriber(int subscriberId, Channel<UpdateRunEntry>? channel)
    {
        if (_subscribers.TryRemove(subscriberId, out var existingChannel))
        {
            existingChannel.Writer.TryComplete();
            return;
        }

        channel?.Writer.TryComplete();
    }
}