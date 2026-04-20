namespace Dyndns.Service.Models;

public sealed record UpdateRunEntry(
    Guid Id,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    long DurationMilliseconds,
    string Trigger,
    bool Succeeded,
    bool Updated,
    string Message,
    string? CurrentIp = null,
    string? PreviousIp = null,
    string? Error = null,
    string? ErrorSummary = null);