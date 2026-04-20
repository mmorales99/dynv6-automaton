namespace Dyndns.Service.Services;

public interface IUpdateFailureNotifier
{
    Task SendAsync(string subject, string body, CancellationToken cancellationToken);
}