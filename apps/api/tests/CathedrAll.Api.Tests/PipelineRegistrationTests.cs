using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;
using CathedrAll.Kernel.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Api.Tests;

public sealed class PipelineRegistrationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string AnyDatabase = "Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x";

    [Fact]
    public void O_anel_de_transacao_deve_ser_o_mais_interno_do_pipeline()
    {
        IPipelineBehavior<PipelineProbeCommand, Result>[] aneis = Aneis<PipelineProbeCommand>();

        Assert.Equal(2, aneis.Length);
        Assert.IsAssignableFrom<TransactionBehavior<PipelineProbeCommand, Result>>(aneis[^1]);
    }

    [Fact]
    public void Consulta_nao_deve_receber_o_anel_de_transacao()
    {
        IPipelineBehavior<PipelineProbeQuery, Result>[] aneis = Aneis<PipelineProbeQuery>();

        Assert.Single(aneis);
    }

    private IPipelineBehavior<TRequest, Result>[] Aneis<TRequest>()
        where TRequest : IRequest<Result>
    {
        using WebApplicationFactory<Program> configured = factory.WithWebHostBuilder(
            builder => builder.UseSetting(
                $"ConnectionStrings:{PostgresHealthCheck.ConnectionName}",
                AnyDatabase));

        using IServiceScope scope = configured.Services.CreateScope();

        return [.. scope.ServiceProvider.GetServices<IPipelineBehavior<TRequest, Result>>()];
    }
}
