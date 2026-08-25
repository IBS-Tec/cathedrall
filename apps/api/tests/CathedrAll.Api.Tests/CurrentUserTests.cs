using CathedrAll.Kernel.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Api.Tests;

public sealed class CurrentUserTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Configurado = "22222222-2222-2222-2222-222222222222";

    [Fact]
    public void Em_desenvolvimento_o_usuario_atual_deve_ser_o_de_desenvolvimento()
    {
        using IServiceScope scope = factory.Services.CreateScope();

        ICurrentUser currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        Assert.IsType<DevelopmentCurrentUser>(currentUser);
    }

    [Fact]
    public void O_identificador_e_o_papel_devem_vir_da_configuracao()
    {
        using WebApplicationFactory<Program> configured = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("CurrentUser:Id", Configurado);
            builder.UseSetting("CurrentUser:Papel", nameof(Papel.Recepcao));
        });

        using IServiceScope scope = configured.Services.CreateScope();

        ICurrentUser currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        Assert.Equal(new Guid(Configurado), currentUser.Id);
        Assert.Equal(Papel.Recepcao, currentUser.Papel);
    }

    [Fact]
    public void Em_producao_o_host_nao_deve_subir_sem_implementacao_real()
    {
        using WebApplicationFactory<Program> production =
            factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() => production.Services.CreateScope());

        Assert.Contains(nameof(ICurrentUser), exception.Message, StringComparison.Ordinal);
    }
}
