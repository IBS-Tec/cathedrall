using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Kernel.Application.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Deve_registrar_o_sender_como_scoped()
    {
        ServiceCollection services = new();

        services.AddKernelApplication();

        ServiceDescriptor descriptor = Assert.Single(services, s => s.ServiceType == typeof(ISender));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void Chamar_duas_vezes_nao_deve_duplicar_o_registro()
    {
        ServiceCollection services = new();

        services.AddKernelApplication();
        services.AddKernelApplication();

        Assert.Single(services, s => s.ServiceType == typeof(ISender));
    }

    [Fact]
    public void Deve_registrar_o_logging_behavior_como_scoped()
    {
        ServiceCollection services = new();

        services.AddLoggingBehavior();

        ServiceDescriptor descriptor =
            Assert.Single(services, s => s.ServiceType == typeof(IPipelineBehavior<,>));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void Chamar_duas_vezes_nao_deve_duplicar_o_logging_behavior()
    {
        ServiceCollection services = new();

        services.AddLoggingBehavior();
        services.AddLoggingBehavior();

        Assert.Single(services, s => s.ServiceType == typeof(IPipelineBehavior<,>));
    }

    [Fact]
    public void Deve_devolver_a_propria_colecao_para_encadeamento()
    {
        ServiceCollection services = new();

        Assert.Same(services, services.AddKernelApplication());
        Assert.Same(services, services.AddLoggingBehavior());
    }

    [Fact]
    public void Resolver_o_sender_fora_de_escopo_deve_lancar_excecao()
    {
        using ServiceProvider provider = Scenario.Build();

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ISender>());
    }
}
