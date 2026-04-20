using Dyndns.Service.Models;
using Dyndns.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dyndns.Service.Tests;

public class UpdateCycleRunnerTests
{
    [Fact]
    public async Task RunAsync_RecordsSuccessfulRun()
    {
        var updateService = new FakeDnsUpdateService(() => Task.FromResult(new UpdateCycleResult(true, "Updated", "203.0.113.10", "198.51.100.25")));
        var historyStore = new FakeUpdateRunHistoryStore();
        var broadcaster = new FakeUpdateRunBroadcaster();
        var runner = new UpdateCycleRunner(updateService, historyStore, broadcaster, NullLogger<UpdateCycleRunner>.Instance);

        var entry = await runner.RunAsync("manual", CancellationToken.None);

        Assert.True(entry.Succeeded);
        Assert.True(entry.Updated);
        Assert.Equal("manual", entry.Trigger);
        Assert.Single(historyStore.Entries);
        Assert.Equal("Updated", historyStore.Entries[0].Message);
        Assert.Single(broadcaster.PublishedEntries);
    }

    [Fact]
    public async Task RunAsync_RecordsFailedRunWhenServiceThrows()
    {
        var updateService = new FakeDnsUpdateService(() => throw new InvalidOperationException("Boom"));
        var historyStore = new FakeUpdateRunHistoryStore();
        var broadcaster = new FakeUpdateRunBroadcaster();
        var runner = new UpdateCycleRunner(updateService, historyStore, broadcaster, NullLogger<UpdateCycleRunner>.Instance);

        var entry = await runner.RunAsync("manual", CancellationToken.None);

        Assert.False(entry.Succeeded);
        Assert.False(entry.Updated);
        Assert.Contains("Boom", entry.Error ?? string.Empty);
        Assert.Contains("Unexpected process error", entry.ErrorSummary ?? string.Empty);
        Assert.Single(historyStore.Entries);
        Assert.False(historyStore.Entries[0].Succeeded);
        Assert.Single(broadcaster.PublishedEntries);
    }

    [Fact]
    public async Task RunAsync_RecordsTimeoutSummaryWhenServiceTimesOut()
    {
        var updateService = new FakeDnsUpdateService(() => throw new TaskCanceledException("The request timed out."));
        var historyStore = new FakeUpdateRunHistoryStore();
        var broadcaster = new FakeUpdateRunBroadcaster();
        var runner = new UpdateCycleRunner(updateService, historyStore, broadcaster, NullLogger<UpdateCycleRunner>.Instance);

        var entry = await runner.RunAsync("manual", CancellationToken.None);

        Assert.False(entry.Succeeded);
        Assert.Equal("Unable to get IP: the provider timed out.", entry.ErrorSummary);
        Assert.Contains("timed out", entry.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Single(broadcaster.PublishedEntries);
    }

    [Fact]
    public async Task RunAsync_ForcesUpdateWhenRequested()
    {
        var updateService = new FakeDnsUpdateService(() => Task.FromResult(new UpdateCycleResult(true, "Updated", "203.0.113.10", "198.51.100.25")));
        var historyStore = new FakeUpdateRunHistoryStore();
        var broadcaster = new FakeUpdateRunBroadcaster();
        var runner = new UpdateCycleRunner(updateService, historyStore, broadcaster, NullLogger<UpdateCycleRunner>.Instance);

        var entry = await runner.RunAsync("manual", CancellationToken.None, forceUpdate: true);

        Assert.True(entry.Succeeded);
        Assert.True(entry.Updated);
        Assert.Equal("Updated", entry.Message);
        Assert.Single(broadcaster.PublishedEntries);
    }

    private sealed class FakeDnsUpdateService : IDnsUpdateService
    {
        private readonly Func<Task<UpdateCycleResult>> _handler;

        public FakeDnsUpdateService(Func<Task<UpdateCycleResult>> handler)
        {
            _handler = handler;
        }

        public Task<UpdateCycleResult> RunOnceAsync(CancellationToken cancellationToken, bool forceUpdate = false)
            => _handler();
    }

    private sealed class FakeUpdateRunHistoryStore : IUpdateRunHistoryStore
    {
        public List<UpdateRunEntry> Entries { get; } = [];

        public Task AppendAsync(UpdateRunEntry entry, CancellationToken cancellationToken)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<UpdateRunEntry>> ReadRecentAsync(int maxEntries, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<UpdateRunEntry>>(Entries.TakeLast(maxEntries).Reverse().ToArray());
    }

    private sealed class FakeUpdateRunBroadcaster : IUpdateRunBroadcaster
    {
        public List<UpdateRunEntry> PublishedEntries { get; } = [];

        public void Publish(UpdateRunEntry entry)
        {
            PublishedEntries.Add(entry);
        }

        public IAsyncEnumerable<UpdateRunEntry> SubscribeAsync(CancellationToken cancellationToken)
            => EmptyAsyncEnumerable();

        private static async IAsyncEnumerable<UpdateRunEntry> EmptyAsyncEnumerable()
        {
            yield break;
        }
    }
}