using Dyndns.Service.Models;

namespace Dyndns.Service.Services;

public sealed class UpdateCycleRunner : IUpdateCycleRunner
{
    private readonly IDnsUpdateService _dnsUpdateService;
    private readonly ILogger<UpdateCycleRunner> _logger;
    private readonly IUpdateRunHistoryStore _historyStore;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public UpdateCycleRunner(
        IDnsUpdateService dnsUpdateService,
        IUpdateRunHistoryStore historyStore,
        ILogger<UpdateCycleRunner> logger)
    {
        _dnsUpdateService = dnsUpdateService;
        _historyStore = historyStore;
        _logger = logger;
    }

    public async Task<UpdateRunEntry> RunAsync(string trigger, CancellationToken cancellationToken, bool forceUpdate = false)
    {
        if (string.IsNullOrWhiteSpace(trigger))
        {
            throw new ArgumentException("A trigger is required.", nameof(trigger));
        }

        await _gate.WaitAsync(cancellationToken);

        var startedAt = DateTimeOffset.UtcNow;
        try
        {
            UpdateRunEntry entry;

            try
            {
                var result = await _dnsUpdateService.RunOnceAsync(cancellationToken, forceUpdate);
                var finishedAt = DateTimeOffset.UtcNow;
                entry = new UpdateRunEntry(
                    Guid.NewGuid(),
                    startedAt,
                    finishedAt,
                    (long)(finishedAt - startedAt).TotalMilliseconds,
                    trigger,
                    true,
                    result.Updated,
                    result.Message,
                    result.CurrentIp,
                    result.PreviousIp);
            }
            catch (OperationCanceledException exception)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                var finishedAt = DateTimeOffset.UtcNow;
                entry = new UpdateRunEntry(
                    Guid.NewGuid(),
                    startedAt,
                    finishedAt,
                    (long)(finishedAt - startedAt).TotalMilliseconds,
                    trigger,
                    false,
                    false,
                    "Update cycle failed.",
                    null,
                    null,
                    exception.ToString(),
                    CreateFailureSummary(exception));

                _logger.LogError(exception, "Dynv6 update cycle timed out.");
            }
            catch (Exception exception)
            {
                var finishedAt = DateTimeOffset.UtcNow;
                entry = new UpdateRunEntry(
                    Guid.NewGuid(),
                    startedAt,
                    finishedAt,
                    (long)(finishedAt - startedAt).TotalMilliseconds,
                    trigger,
                    false,
                    false,
                    "Update cycle failed.",
                    null,
                    null,
                    exception.ToString(),
                    CreateFailureSummary(exception));

                _logger.LogError(exception, "Dynv6 update cycle failed.");
            }

            await TryAppendAsync(entry, cancellationToken);
            return entry;
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<IReadOnlyList<UpdateRunEntry>> ReadRecentRunsAsync(int maxEntries, CancellationToken cancellationToken)
        => _historyStore.ReadRecentAsync(maxEntries, cancellationToken);

    private async Task TryAppendAsync(UpdateRunEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            await _historyStore.AppendAsync(entry, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Dynv6 run history could not be saved.");
        }
    }

    private static string CreateFailureSummary(Exception exception)
    {
        if (exception is TaskCanceledException or TimeoutException)
        {
            return "Unable to get IP: the provider timed out.";
        }

        if (exception is OperationCanceledException)
        {
            return "Run cancelled.";
        }

        if (exception is InvalidOperationException invalidOperationException)
        {
            var message = invalidOperationException.Message;

            if (message.Contains("request failed", StringComparison.OrdinalIgnoreCase))
            {
                return "Dynv6 request failed.";
            }

            if (message.Contains("response was empty", StringComparison.OrdinalIgnoreCase))
            {
                return "Unable to get IP: the provider returned an empty response.";
            }

            if (message.Contains("not an IPv4 address", StringComparison.OrdinalIgnoreCase))
            {
                return "Unable to get IP: the provider returned an invalid IPv4 address.";
            }
        }

        return $"Unexpected process error: {exception.GetType().Name}.";
    }

}