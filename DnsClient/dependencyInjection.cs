using DnsClient;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDnsClientFactory(this IServiceCollection services) 
    {
        services.AddSingleton<DnsClientFactory>();
        return services;
    }
}
