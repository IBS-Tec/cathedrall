using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CathedrAll.Api.Tests;

public sealed class HealthTests(WebApplicationFactory<Program> fabrica)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Health_responde_200_com_corpo_Healthy()
    {
        HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage resposta = await cliente.GetAsync(new Uri("/health", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("Healthy", await resposta.Content.ReadAsStringAsync());
    }
}
