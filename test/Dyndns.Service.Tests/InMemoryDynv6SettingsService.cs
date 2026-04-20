using Dyndns.Service.Options;
using Dyndns.Service.Services;

namespace Dyndns.Service.Tests;

internal sealed class InMemoryDynv6SettingsService : IDynv6SettingsService
{
    private Dynv6Options _settings;

    public InMemoryDynv6SettingsService(Dynv6Options settings)
    {
        _settings = Clone(settings);
    }

    public Task<Dynv6Options> GetAsync(CancellationToken cancellationToken)
        => Task.FromResult(Clone(_settings));

    public Task UpdateAsync(Dynv6Options settings, CancellationToken cancellationToken)
    {
        _settings = Clone(settings);
        return Task.CompletedTask;
    }

    private static Dynv6Options Clone(Dynv6Options settings)
        => new()
        {
            ZoneName = settings.ZoneName,
            Key = settings.Key,
            ForceUpdate = settings.ForceUpdate,
            ScheduledEvery = settings.ScheduledEvery,
            LastPublicIpPath = settings.LastPublicIpPath,
            RunHistoryPath = settings.RunHistoryPath,
            RuntimeSettingsPath = settings.RuntimeSettingsPath,
            PublicIpUrl = settings.PublicIpUrl,
            DyndnsApiUrl = settings.DyndnsApiUrl,
        };
}