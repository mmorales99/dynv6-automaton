namespace Dyndns.Service.Services;

public sealed class UpdateFailurePolicy : IUpdateFailurePolicy
{
    private static readonly TimeSpan HealthyDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromHours(1);
    private static readonly TimeSpan DaytimeNotificationDelay = TimeSpan.FromHours(1);
    private static readonly TimeSpan NighttimeNotificationDelay = TimeSpan.FromHours(8);
    private const int DaytimeStartHour = 8;
    private const int NighttimeStartHour = 22;

    public TimeSpan GetHealthyDelay() => HealthyDelay;

    public TimeSpan GetRetryDelay() => RetryDelay;

    public bool ShouldNotify(DateTimeOffset now, DateTimeOffset? lastNotificationAt)
    {
        if (!lastNotificationAt.HasValue)
        {
            return true;
        }

        var elapsed = now - lastNotificationAt.Value;
        if (elapsed < TimeSpan.Zero)
        {
            return true;
        }

        var notificationDelay = IsDaytime(now) ? DaytimeNotificationDelay : NighttimeNotificationDelay;
        return elapsed >= notificationDelay;
    }

    private static bool IsDaytime(DateTimeOffset now)
        => now.Hour >= DaytimeStartHour && now.Hour < NighttimeStartHour;
}