using Dyndns.Service.Options;
using Dyndns.Service.Services;
using Microsoft.Extensions.Options;

namespace Dyndns.Service.Tests;

public class Dynv6SettingsServiceTests
{
    [Fact]
    public async Task UpdateAsync_PersistsSettingsToDisk()
    {
        var runtimeFilePath = Path.Combine(Path.GetTempPath(), $"dynv6-settings-{Guid.NewGuid():N}.json");

        try
        {
            var settings = new Dynv6Options
            {
                ZoneName = "example.com",
                Key = "secret",
                RunHistoryPath = runtimeFilePath,
                LastPublicIpPath = runtimeFilePath,
                RuntimeSettingsPath = runtimeFilePath,
                DyndnsApiUrl = "https://dynv6.com/api/"
            };

            var service = new FileDynv6SettingsService(Microsoft.Extensions.Options.Options.Create(settings));
            var updated = new Dynv6Options
            {
                ZoneName = "changed.example.com",
                Key = "changed-secret",
                ForceUpdate = true,
                LastPublicIpPath = runtimeFilePath,
                RunHistoryPath = runtimeFilePath,
                RuntimeSettingsPath = runtimeFilePath,
                PublicIpUrl = "https://api.ipify.org?format=json",
                DyndnsApiUrl = "https://dynv6.com/api/"
            };

            await service.UpdateAsync(updated, CancellationToken.None);

            var reloaded = await service.GetAsync(CancellationToken.None);

            Assert.Equal("changed.example.com", reloaded.ZoneName);
            Assert.True(File.Exists(runtimeFilePath));
        }
        finally
        {
            if (File.Exists(runtimeFilePath))
            {
                File.Delete(runtimeFilePath);
            }
        }
    }
}