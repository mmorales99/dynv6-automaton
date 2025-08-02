using DnsClient;
using Helpers;
using Microsoft.Extensions.Options;
using Service.src.Options;

namespace Service.src;

public class Worker(
    ILogger<Worker> logger,
    IOptions<DNSUpdaterOptions> options,
    DnsClientFactory dnsClientFactory,
    IPHelper ipHelper
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            var hasPublicIpChanged = ipHelper.CheckPublicIP(out string? ip);
            dnsClientFactory.UpdateDNSRecord(options.Value.DnsClient, hasPublicIpChanged, ip);
            await Task.Delay(TimeSpan.FromSeconds(30));
        }
    }
}