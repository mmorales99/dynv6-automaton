using System.Text.Json;
using Dyndns.Service.Options;
using Microsoft.Extensions.Options;

namespace Dyndns.Service.Services;

public sealed class FileDynv6SettingsService : IDynv6SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath;
    private Dynv6Options _current;

    public FileDynv6SettingsService(IOptions<Dynv6Options> options)
    {
        _current = Clone(options.Value);
        _filePath = options.Value.RuntimeSettingsPath;

        LoadPersistedSettings();
    }

    public async Task<Dynv6Options> GetAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return Clone(_current);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpdateAsync(Dynv6Options settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ValidateSettings(settings);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            _current = Clone(settings);
            await SavePersistedSettingsAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private void LoadPersistedSettings()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        var json = File.ReadAllText(_filePath);
        var persistedSettings = JsonSerializer.Deserialize<Dynv6Options>(json, JsonOptions);
        if (persistedSettings is not null)
        {
            _current = Clone(persistedSettings);
        }
    }

    private async Task SavePersistedSettingsAsync(CancellationToken cancellationToken)
    {
        var directoryPath = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(_current, JsonOptions), cancellationToken);
    }

    private static Dynv6Options Clone(Dynv6Options settings)
        => new()
        {
            ZoneName = settings.ZoneName,
            Key = settings.Key,
            ForceUpdate = settings.ForceUpdate,
            LastPublicIpPath = settings.LastPublicIpPath,
            RunHistoryPath = settings.RunHistoryPath,
            RuntimeSettingsPath = settings.RuntimeSettingsPath,
            ScheduledEvery = settings.ScheduledEvery,
            PublicIpUrl = settings.PublicIpUrl,
            DyndnsApiUrl = settings.DyndnsApiUrl,
        };

    private static void ValidateSettings(Dynv6Options settings)
    {
        if (settings.ScheduledEvery <= TimeSpan.Zero)
        {
            throw new ArgumentException("Dynv6:ScheduledEvery must be greater than zero.", nameof(settings));
        }
    }
}