using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Kernel.Application.Tests;

internal static class Cenario
{
    public static ServiceProvider Construir(Action<IServiceCollection>? registrar = null)
    {
        ServiceCollection servicos = new();

        servicos.AddKernelApplication();
        registrar?.Invoke(servicos);

        return servicos.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    public static ISender SenderDoEscopo(IServiceScope escopo) =>
        escopo.ServiceProvider.GetRequiredService<ISender>();
}
