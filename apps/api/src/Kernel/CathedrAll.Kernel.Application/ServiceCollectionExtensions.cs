using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CathedrAll.Kernel.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKernelApplication(this IServiceCollection services)
    {
        services.TryAddScoped<ISender, Sender>();
        return services;
    }
}
