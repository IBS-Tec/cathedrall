using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CathedrAll.Api.Tests;

public sealed class HealthTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string UnreachableDatabase =
        "Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x;Timeout=1";

    [Fact]
    public async Task Health_responde_200_com_corpo_Healthy()
    {
        HttpResponseMessage response = await Call("/health", UnreachableDatabase);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "Healthy",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Health_ready_responde_503_quando_o_banco_esta_inacessivel()
    {
        HttpResponseMessage response = await Call("/health/ready", UnreachableDatabase);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(
            "Unhealthy",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Health_ready_responde_503_quando_a_connection_string_nao_existe()
    {
        HttpResponseMessage response = await Call("/health/ready", connectionString: null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Health_nao_expoe_detalhe_de_dependencia_no_corpo()
    {
        HttpResponseMessage response = await Call("/health/ready", UnreachableDatabase);

        string body =
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.DoesNotContain("postgres", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("127.0.0.1", body, StringComparison.Ordinal);
    }

    private async Task<HttpResponseMessage> Call(string route, string? connectionString)
    {
        using WebApplicationFactory<Program> configured = factory.WithWebHostBuilder(
            builder => builder.UseSetting(
                $"ConnectionStrings:{PostgresHealthCheck.ConnectionName}",
                connectionString));

        HttpClient client = configured.CreateClient();

        return await client.GetAsync(
            new Uri(route, UriKind.Relative),
            TestContext.Current.CancellationToken);
    }
}
