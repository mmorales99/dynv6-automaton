using Dyndns.Service.Services;

namespace Dyndns.Service.Tests;

public class IpChangeCheckerTests
{
    [Theory]
    [InlineData(null, "203.0.113.10", true)]
    [InlineData("203.0.113.10", "203.0.113.10", false)]
    [InlineData("203.0.113.10", "198.51.100.25", true)]
    public async Task CheckAsync_ReportsWhetherTheIpChanged(string? previousIp, string currentIp, bool expectedHasChanged)
    {
        var checker = new IpChangeChecker(
            new FakePublicIpProvider(currentIp),
            new FakeUpdateStateStore(previousIp));

        var result = await checker.CheckAsync(CancellationToken.None);

        Assert.Equal(expectedHasChanged, result.HasChanged);
        Assert.Equal(currentIp, result.CurrentIp);
        Assert.Equal(previousIp, result.PreviousIp);
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

    private sealed class FakeUpdateStateStore : IUpdateStateStore
    {
        private readonly string? _previousIp;

        public FakeUpdateStateStore(string? previousIp)
        {
            _previousIp = previousIp;
        }

        public Task<string?> ReadLastKnownIpAsync(CancellationToken cancellationToken)
            => Task.FromResult(_previousIp);

        public Task SaveLastKnownIpAsync(string ipAddress, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}