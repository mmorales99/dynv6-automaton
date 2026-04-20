using Dyndns.Service.Models;
using Dyndns.Service.Options;
using Dyndns.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dyndns.Service.Tests;

public class DnsUpdateServiceTests
{
    [Fact]
    public async Task RunOnceAsync_SkipsWhenIpIsUnchanged_UnlessForced()
    {
        var settingsService = new FakeSettingsService(new Dynv6Options
        {
            ZoneName = "example.com",
            Key = "secret",
            ForceUpdate = false
        });

        var ipChangeManager = new FakeIpChangeManager(new IpChangeCheckResult(false, "203.0.113.10", "203.0.113.10"));
        var service = new DnsUpdateService(ipChangeManager, NullLogger<DnsUpdateService>.Instance, settingsService);

        var skipped = await service.RunOnceAsync(CancellationToken.None);
        var forced = await service.RunOnceAsync(CancellationToken.None, forceUpdate: true);

        Assert.False(skipped.Updated);
        Assert.Equal("IPv4 address unchanged.", skipped.Message);
        Assert.True(forced.Updated);
        Assert.Equal(1, ipChangeManager.UpdateCalls);
    }

    private sealed class FakeSettingsService : IDynv6SettingsService
    {
        private readonly Dynv6Options _settings;

        public FakeSettingsService(Dynv6Options settings)
        {
            _settings = settings;
        }

        public bool IsInitialized => true;

        public Task<Dynv6Options> GetAsync(CancellationToken cancellationToken)
            => Task.FromResult(_settings);

        public Task UpdateAsync(Dynv6Options settings, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeIpChangeManager : IIpChangeManager
    {
        private readonly IpChangeCheckResult _checkResult;

        public FakeIpChangeManager(IpChangeCheckResult checkResult)
        {
            _checkResult = checkResult;
        }

        public int UpdateCalls { get; private set; }

        public Task<IpChangeCheckResult> CheckAsync(CancellationToken cancellationToken)
            => Task.FromResult(_checkResult);

        public Task<UpdateCycleResult> UpdateIpAsync(string zoneName, string newIp, CancellationToken cancellationToken)
        {
            UpdateCalls++;
            return Task.FromResult(new UpdateCycleResult(true, "Updated", newIp, _checkResult.PreviousIp));
        }
    }
}