using Dyndns.Service.Options;

namespace Dyndns.Service.Services;

public interface IDynv6SettingsService
{
    Task<Dynv6Options> GetAsync(CancellationToken cancellationToken);

    Task UpdateAsync(Dynv6Options settings, CancellationToken cancellationToken);
}