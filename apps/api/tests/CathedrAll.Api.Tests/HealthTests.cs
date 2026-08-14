using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CathedrAll.Api.Tests;

public sealed class HealthTests(WebApplicationFactory<Program> fabrica)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BancoInacessivel =
        "Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x;Timeout=1";

    [Fact]
    public async Task Health_responde_200_com_corpo_Healthy()
    {
        HttpResponseMessage resposta = await Chamar("/health", BancoInacessivel);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(
            "Healthy",
            await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Health_ready_responde_503_quando_o_banco_esta_inacessivel()
    {
        HttpResponseMessage resposta = await Chamar("/health/ready", BancoInacessivel);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
        Assert.Equal(
            "Unhealthy",
            await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Health_ready_responde_503_quando_a_connection_string_nao_existe()
    {
        HttpResponseMessage resposta = await Chamar("/health/ready", conexao: null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
    }

    [Fact]
    public async Task Health_nao_expoe_detalhe_de_dependencia_no_corpo()
    {
        HttpResponseMessage resposta = await Chamar("/health/ready", BancoInacessivel);

        string corpo =
            await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.DoesNotContain("postgres", corpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("127.0.0.1", corpo, StringComparison.Ordinal);
    }

    private async Task<HttpResponseMessage> Chamar(string rota, string? conexao)
    {
        using WebApplicationFactory<Program> comConfiguracao = fabrica.WithWebHostBuilder(
            construtor => construtor.UseSetting(
                $"ConnectionStrings:{PostgresHealthCheck.NomeDaConexao}",
                conexao));

        HttpClient cliente = comConfiguracao.CreateClient();

        return await cliente.GetAsync(
            new Uri(rota, UriKind.Relative),
            TestContext.Current.CancellationToken);
    }
}
