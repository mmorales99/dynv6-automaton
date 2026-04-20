using Dyndns.Service.Services;

namespace Dyndns.Service.Tests;

public class UpdateFailurePolicyTests
{
    [Fact]
    public void GetHealthyDelay_ReturnsFiveMinutes()
    {
        var policy = new UpdateFailurePolicy();

        Assert.Equal(TimeSpan.FromMinutes(5), policy.GetHealthyDelay());
    }

    [Fact]
    public void GetRetryDelay_ReturnsOneHour()
    {
        var policy = new UpdateFailurePolicy();

        Assert.Equal(TimeSpan.FromHours(1), policy.GetRetryDelay());
    }

    [Fact]
    public void ShouldNotify_ReturnsTrueForFirstFailure()
    {
        var policy = new UpdateFailurePolicy();

        Assert.True(policy.ShouldNotify(new DateTimeOffset(2026, 4, 18, 10, 0, 0, TimeSpan.Zero), null));
    }

    [Fact]
    public void ShouldNotify_UsesOneHourDuringDaytime()
    {
        var policy = new UpdateFailurePolicy();
        var lastNotificationAt = new DateTimeOffset(2026, 4, 18, 10, 0, 0, TimeSpan.Zero);

        Assert.False(policy.ShouldNotify(new DateTimeOffset(2026, 4, 18, 10, 30, 0, TimeSpan.Zero), lastNotificationAt));
        Assert.True(policy.ShouldNotify(new DateTimeOffset(2026, 4, 18, 11, 0, 0, TimeSpan.Zero), lastNotificationAt));
    }

    [Fact]
    public void ShouldNotify_UsesEightHoursDuringNighttime()
    {
        var policy = new UpdateFailurePolicy();
        var lastNotificationAt = new DateTimeOffset(2026, 4, 18, 23, 0, 0, TimeSpan.Zero);

        Assert.False(policy.ShouldNotify(new DateTimeOffset(2026, 4, 19, 3, 0, 0, TimeSpan.Zero), lastNotificationAt));
        Assert.True(policy.ShouldNotify(new DateTimeOffset(2026, 4, 19, 7, 0, 0, TimeSpan.Zero), lastNotificationAt));
    }
}