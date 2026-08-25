using CathedrAll.Kernel.Application;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CathedrAll.Api;

internal static class CurrentUserRegistration
{
    public static IServiceCollection AddDevelopmentCurrentUser(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DevelopmentCurrentUserOptions>(
            configuration.GetSection(DevelopmentCurrentUserOptions.SectionName));

        services.TryAddScoped<ICurrentUser, DevelopmentCurrentUser>();

        return services;
    }

    public static IServiceCollection RequireCurrentUser(this IServiceCollection services)
    {
        if (services.Any(service => service.ServiceType == typeof(ICurrentUser)))
        {
            return services;
        }

        throw new InvalidOperationException(
            $"Nenhuma implementação de {nameof(ICurrentUser)} foi registrada. A implementação " +
            "de desenvolvimento só entra fora de produção; em produção o host exige uma real.");
    }
}
