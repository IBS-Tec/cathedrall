using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Kernel.Application.Tests;

internal static class Scenario
{
    public static ServiceProvider Build(Action<IServiceCollection>? register = null)
    {
        ServiceCollection services = new();

        services.AddKernelApplication();
        register?.Invoke(services);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    public static ISender SenderFrom(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<ISender>();
}
