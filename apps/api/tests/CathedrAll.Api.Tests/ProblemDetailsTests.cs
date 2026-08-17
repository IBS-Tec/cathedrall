using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CathedrAll.Api.Tests;

public sealed class ProblemDetailsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Rota_inexistente_responde_404_em_problem_json()
    {
        HttpResponseMessage response = await Get("/rota-que-nao-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Problem_json_carrega_o_traceId()
    {
        HttpResponseMessage response = await Get("/rota-que-nao-existe");

        string body =
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"traceId\"", body, StringComparison.Ordinal);
    }

    private async Task<HttpResponseMessage> Get(string route)
    {
        HttpClient client = factory.CreateClient();

        return await client.GetAsync(
            new Uri(route, UriKind.Relative),
            TestContext.Current.CancellationToken);
    }
}
