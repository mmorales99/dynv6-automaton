using System.Text.Json;
using Dyndns.Service.Models;

namespace Dyndns.Service.Services;

public sealed class FileUpdateRunHistoryStore : IUpdateRunHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDynv6SettingsService _settingsService;

    public FileUpdateRunHistoryStore(IDynv6SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task AppendAsync(UpdateRunEntry entry, CancellationToken cancellationToken)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        var filePath = (await _settingsService.GetAsync(cancellationToken)).RunHistoryPath;
        var directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var line = JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine;
        await File.AppendAllTextAsync(filePath, line, cancellationToken);
    }

    public async Task<IReadOnlyList<UpdateRunEntry>> ReadRecentAsync(int maxEntries, CancellationToken cancellationToken)
    {
        if (maxEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntries));
        }

        var filePath = (await _settingsService.GetAsync(cancellationToken)).RunHistoryPath;

        if (!File.Exists(filePath))
        {
            return [];
        }

        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        return lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<UpdateRunEntry>(line, JsonOptions))
            .Where(entry => entry is not null)
            .Cast<UpdateRunEntry>()
            .TakeLast(maxEntries)
            .Reverse()
            .ToArray();
    }
}