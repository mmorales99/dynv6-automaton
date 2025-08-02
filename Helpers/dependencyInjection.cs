using Helpers;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHelpers(this IServiceCollection services) 
    {
        services.AddSingleton<IPHelper>();
        return services;
    }
}
