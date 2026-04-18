namespace Dyndns.Service.Models;

public sealed record IpChangeCheckResult(
    bool HasChanged,
    string CurrentIp,
    string? PreviousIp);