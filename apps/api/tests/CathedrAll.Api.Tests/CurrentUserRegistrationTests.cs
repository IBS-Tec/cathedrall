using CathedrAll.Kernel.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Api.Tests;

public sealed class CurrentUserRegistrationTests
{
    private static readonly IConfiguration Empty = new ConfigurationBuilder().Build();

    [Fact]
    public void Deve_registrar_o_usuario_de_desenvolvimento_como_scoped()
    {
        ServiceCollection services = new();

        services.AddDevelopmentCurrentUser(Empty);

        ServiceDescriptor descriptor =
            Assert.Single(services, s => s.ServiceType == typeof(ICurrentUser));

        Assert.Equal(typeof(DevelopmentCurrentUser), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void Nao_deve_sobrescrever_uma_implementacao_ja_registrada()
    {
        ServiceCollection services = new();

        services.AddScoped<ICurrentUser, FakeCurrentUser>();
        services.AddDevelopmentCurrentUser(Empty);

        ServiceDescriptor descriptor =
            Assert.Single(services, s => s.ServiceType == typeof(ICurrentUser));

        Assert.Equal(typeof(FakeCurrentUser), descriptor.ImplementationType);
    }

    [Fact]
    public void Deve_lancar_quando_nenhuma_implementacao_esta_registrada()
    {
        ServiceCollection services = new();

        Assert.Throws<InvalidOperationException>(services.RequireCurrentUser);
    }

    [Fact]
    public void Nao_deve_lancar_quando_ha_qualquer_implementacao_registrada()
    {
        ServiceCollection services = new();

        services.AddScoped<ICurrentUser, FakeCurrentUser>();

        Assert.Same(services, services.RequireCurrentUser());
    }
}
