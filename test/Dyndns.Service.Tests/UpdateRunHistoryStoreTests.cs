using Dyndns.Service.Models;
using Dyndns.Service.Options;
using Dyndns.Service.Services;

namespace Dyndns.Service.Tests;

public class UpdateRunHistoryStoreTests
{
    [Fact]
    public async Task AppendAndReadRecentAsync_ReturnsNewestEntriesFirst()
    {
        var historyFilePath = Path.Combine(Path.GetTempPath(), $"dyndns-history-{Guid.NewGuid():N}.jsonl");

        try
        {
            var store = new FileUpdateRunHistoryStore(new InMemoryDynv6SettingsService(new Dynv6Options
            {
                RunHistoryPath = historyFilePath
            }));

            var first = CreateEntry("scheduled", "First");
            var second = CreateEntry("manual", "Second");

            await store.AppendAsync(first, CancellationToken.None);
            await store.AppendAsync(second, CancellationToken.None);

            var entries = await store.ReadRecentAsync(10, CancellationToken.None);

            Assert.Equal(2, entries.Count);
            Assert.Equal("Second", entries[0].Message);
            Assert.Equal("First", entries[1].Message);
        }
        finally
        {
            if (File.Exists(historyFilePath))
            {
                File.Delete(historyFilePath);
            }
        }
    }

    private static UpdateRunEntry CreateEntry(string trigger, string message)
        => new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddSeconds(-5),
            DateTimeOffset.UtcNow,
            500,
            trigger,
            true,
            true,
            message,
            "203.0.113.10",
            "198.51.100.25");
}