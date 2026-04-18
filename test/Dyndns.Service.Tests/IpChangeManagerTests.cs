using System.Net;
using Dyndns.Service.Models;
using Dyndns.Service.Options;
using Dyndns.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Dyndns.Service.Tests;

public class IpChangeManagerTests
{
    [Fact]
    public async Task UpdateIpAsync_UsesZoneNameAndNewIpToUpdateWildcardRecord()
    {
        var dynv6Client = new FakeDynv6Client();
        var stateStore = new FakeUpdateStateStore();
        var checker = new IpChangeChecker(
            new FakePublicIpProvider("203.0.113.10"),
            new FakeUpdateStateStore(null));
        var manager = new IpChangeManager(
            checker,
            dynv6Client,
            NullLogger<IpChangeManager>.Instance,
            Microsoft.Extensions.Options.Options.Create(new Dynv6Options { RecordName = "*" }),
            stateStore);

        var changeCheck = await manager.CheckAsync(CancellationToken.None);
        var result = await manager.UpdateIpAsync("example.com", "203.0.113.10", CancellationToken.None);

        Assert.True(changeCheck.HasChanged);
        Assert.Equal("203.0.113.10", changeCheck.CurrentIp);
        Assert.True(result.Updated);
        Assert.Equal("example.com", dynv6Client.LastZoneName);
        Assert.Equal("203.0.113.10", dynv6Client.LastUpdatedIp);
        Assert.Equal("203.0.113.10", stateStore.LastSavedIp);
    }

    private sealed class FakePublicIpProvider : IPublicIpProvider
    {
        private readonly string _ip;

        public FakePublicIpProvider(string ip)
        {
            _ip = ip;
        }

        public Task<string> GetCurrentIpv4AddressAsync(CancellationToken cancellationToken)
            => Task.FromResult(_ip);
    }

    private sealed class FakeDynv6Client : IDynv6Client
    {
        public string? LastZoneName { get; private set; }

        public string? LastUpdatedIp { get; private set; }

        public Task<Dynv6Zone> GetZoneByNameAsync(string zoneName, CancellationToken cancellationToken)
        {
            LastZoneName = zoneName;
            return Task.FromResult(new Dynv6Zone { Id = 42, Name = zoneName });
        }

        public Task<IReadOnlyList<Dynv6Record>> GetRecordsAsync(long zoneId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Dynv6Record>>(
                [
                    new Dynv6Record
                    {
                        Id = 7,
                        ZoneId = zoneId,
                        Name = "*",
                        Type = "A",
                        Data = "198.51.100.1"
                    }
                ]);

        public Task<Dynv6Record> UpdateRecordAsync(long zoneId, long recordId, Dynv6RecordUpdateRequest request, CancellationToken cancellationToken)
        {
            LastUpdatedIp = request.Data;

            return Task.FromResult(new Dynv6Record
            {
                Id = recordId,
                ZoneId = zoneId,
                Name = request.Name,
                Type = request.Type,
                Data = request.Data
            });
        }
    }

    private sealed class FakeUpdateStateStore : IUpdateStateStore
    {
        private readonly string? _previousIp;

        public string? LastSavedIp { get; private set; }

        public FakeUpdateStateStore(string? previousIp = null)
        {
            _previousIp = previousIp;
        }

        public Task<string?> ReadLastKnownIpAsync(CancellationToken cancellationToken)
            => Task.FromResult(_previousIp);

        public Task SaveLastKnownIpAsync(string ipAddress, CancellationToken cancellationToken)
        {
            LastSavedIp = ipAddress;
            return Task.CompletedTask;
        }
    }
}