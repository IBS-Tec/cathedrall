using System.Net;
using System.Text.RegularExpressions;
using CathedrAll.Pessoas.Domain;

namespace CathedrAll.Api.Tests;

public sealed class AniversariantesEndpointTests
{
    private static readonly DateOnly Chegada = new(2024, 3, 12);

    [Fact]
    public async Task Resposta_deve_ter_o_envelope_do_exemplo_da_secao_6()
    {
        await using PessoasApiFactory factory = new();
        HttpClient client = await factory.SemearAsync(
            Nova("Maria Souza", nascimento: new DateOnly(1990, 8, 25)),
            Nova("João Guedes", casamento: new DateOnly(2015, 8, 27)));

        string corpo = await BuscarAsync(client, "2026-08-23", "2026-08-29");

        Assert.Equal(
            """
            {"aniversariantes":[{"id":"<id>","nome":"Maria Souza","tipo":"Nascimento","data":"2026-08-25"},{"id":"<id>","nome":"João Guedes","tipo":"Casamento","data":"2026-08-27"}]}
            """,
            SemOsIds(corpo));
    }

    [Fact]
    public async Task A_rota_nao_deve_ser_confundida_com_a_ficha_de_uma_pessoa()
    {
        await using PessoasApiFactory factory = new();
        HttpClient client = await factory.SemearAsync(
            Nova("Maria Souza", nascimento: new DateOnly(1990, 8, 25)));

        HttpResponseMessage response = await client.GetAsync(
            new Uri("/api/pessoas/aniversariantes?from=2026-08-23&to=2026-08-29", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Contains(
            "Maria Souza",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/api/pessoas/aniversariantes")]
    [InlineData("/api/pessoas/aniversariantes?from=2026-08-23")]
    [InlineData("/api/pessoas/aniversariantes?from=2026-08-23&to=domingo")]
    public async Task Intervalo_ausente_ou_ilegivel_deve_responder_400(string rota)
    {
        await using PessoasApiFactory factory = new();
        HttpClient client = await factory.SemearAsync(Nova("Maria Souza"));

        HttpResponseMessage response = await client.GetAsync(
            new Uri(rota, UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Corpo_nao_deve_carregar_dado_pessoal_alem_do_nome()
    {
        await using PessoasApiFactory factory = new();

        HttpClient client = await factory.SemearAsync(
            new Pessoa(new PessoaId(Guid.CreateVersion7()), "João Guedes")
            {
                Celular = new Celular("+5581999998888"),
                Email = new Email("joao@exemplo.com"),
                DataNascimento = new DateOnly(1990, 8, 25),
                Endereco = new Endereco(null, null, null, null, "Boa Viagem", null, null),
            });

        string corpo = await BuscarAsync(client, "2026-08-23", "2026-08-29");

        Assert.Contains("João Guedes", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("999998888", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("joao@exemplo.com", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("1990", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("Boa Viagem", corpo, StringComparison.Ordinal);
    }

    private static Pessoa Nova(
        string nome,
        DateOnly? nascimento = null,
        DateOnly? casamento = null)
    {
        Pessoa pessoa = new(new PessoaId(Guid.CreateVersion7()), nome)
        {
            DataNascimento = nascimento,
            DataCasamento = casamento,
        };

        pessoa.SucederVinculo(Situacao.Visitante, Chegada, null, Chegada);

        return pessoa;
    }

    private static string SemOsIds(string corpo) =>
        Regex.Replace(
            corpo,
            @"""id"":""[0-9a-f-]{36}""",
            @"""id"":""<id>""");

    private static async Task<string> BuscarAsync(HttpClient client, string from, string to)
    {
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"/api/pessoas/aniversariantes?from={from}&to={to}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }
}
