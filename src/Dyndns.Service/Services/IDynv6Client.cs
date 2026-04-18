using Dyndns.Service.Models;

namespace Dyndns.Service.Services;

public interface IDynv6Client
{
    Task<Dynv6Zone> GetZoneByNameAsync(string zoneName, CancellationToken cancellationToken);

    Task<IReadOnlyList<Dynv6Record>> GetRecordsAsync(long zoneId, CancellationToken cancellationToken);

    Task<Dynv6Record> UpdateRecordAsync(long zoneId, long recordId, Dynv6RecordUpdateRequest request, CancellationToken cancellationToken);
}