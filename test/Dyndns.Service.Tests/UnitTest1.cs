using Dyndns.Service.Domain;

namespace Dyndns.Service.Tests;

public class UpdateDecisionTests
{
    [Theory]
    [InlineData(null, "192.168.0.10", false, true)]
    [InlineData("192.168.0.10", "192.168.0.10", false, false)]
    [InlineData("192.168.0.10", "203.0.113.15", false, true)]
    [InlineData("192.168.0.10", "192.168.0.10", true, true)]
    public void ShouldUpdate_ReturnsExpectedResult(string? lastKnownIp, string currentIp, bool forceUpdate, bool expected)
    {
        var result = UpdateDecision.ShouldUpdate(lastKnownIp, currentIp, forceUpdate);

        Assert.Equal(expected, result);
    }
}