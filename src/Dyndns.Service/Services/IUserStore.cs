using Dyndns.Service.Models;

namespace Dyndns.Service.Services;

public interface IUserStore
{
    bool IsInitialized { get; }

    Task InitializeAsync(string adminPassword, string viewerPassword, CancellationToken cancellationToken);

    Task<AuthenticatedUser?> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken);
}
