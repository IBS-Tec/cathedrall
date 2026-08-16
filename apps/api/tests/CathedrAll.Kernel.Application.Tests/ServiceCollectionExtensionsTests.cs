using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Kernel.Application.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Deve_registrar_o_sender_como_scoped()
    {
        ServiceCollection servicos = new();

        servicos.AddKernelApplication();

        ServiceDescriptor descritor = Assert.Single(servicos, s => s.ServiceType == typeof(ISender));

        Assert.Equal(ServiceLifetime.Scoped, descritor.Lifetime);
    }

    [Fact]
    public void Chamar_duas_vezes_nao_deve_duplicar_o_registro()
    {
        ServiceCollection servicos = new();

        servicos.AddKernelApplication();
        servicos.AddKernelApplication();

        Assert.Single(servicos, s => s.ServiceType == typeof(ISender));
    }

    [Fact]
    public void Deve_registrar_o_logging_behavior_como_scoped()
    {
        ServiceCollection servicos = new();

        servicos.AddLoggingBehavior();

        ServiceDescriptor descritor =
            Assert.Single(servicos, s => s.ServiceType == typeof(IPipelineBehavior<,>));

        Assert.Equal(ServiceLifetime.Scoped, descritor.Lifetime);
    }

    [Fact]
    public void Chamar_duas_vezes_nao_deve_duplicar_o_logging_behavior()
    {
        ServiceCollection servicos = new();

        servicos.AddLoggingBehavior();
        servicos.AddLoggingBehavior();

        Assert.Single(servicos, s => s.ServiceType == typeof(IPipelineBehavior<,>));
    }

    [Fact]
    public void Deve_devolver_a_propria_colecao_para_encadeamento()
    {
        ServiceCollection servicos = new();

        Assert.Same(servicos, servicos.AddKernelApplication());
        Assert.Same(servicos, servicos.AddLoggingBehavior());
    }

    [Fact]
    public void Resolver_o_sender_fora_de_escopo_deve_lancar_excecao()
    {
        using ServiceProvider provedor = Cenario.Construir();

        Assert.Throws<InvalidOperationException>(() => provedor.GetRequiredService<ISender>());
    }
}
