namespace Dyndns.Service.Services;

public interface IUpdateFailurePolicy
{
    TimeSpan GetHealthyDelay();

    TimeSpan GetRetryDelay();

    bool ShouldNotify(DateTimeOffset now, DateTimeOffset? lastNotificationAt);
}