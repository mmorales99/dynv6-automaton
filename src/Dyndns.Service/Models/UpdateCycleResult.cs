namespace Dyndns.Service.Models;

public sealed record UpdateCycleResult(
    bool Updated,
    string Message,
    string? CurrentIp = null,
    string? PreviousIp = null);