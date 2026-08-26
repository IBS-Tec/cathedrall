using System.Net;
using CathedrAll.Pessoas.Domain;

namespace CathedrAll.Api.Tests;

public sealed class SearchEndpointTests
{
    private static readonly DateOnly Chegada = new(2024, 3, 12);

    [Theory]
    [InlineData("/api/pessoas/search?q=zuza")]
    [InlineData("/api/pessoas/search?q=")]
    [InlineData("/api/pessoas/search")]
    public async Task Busca_sem_resultado_deve_responder_200_com_lista_vazia(string rota)
    {
        await using PessoasApiFactory factory = new();
        HttpClient client = await factory.SemearAsync(Nova("João Guedes"));

        HttpResponseMessage response = await client.GetAsync(
            new Uri(rota, UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Equal(
            """{"results":[]}""",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Resposta_deve_ter_a_forma_do_exemplo_da_secao_6()
    {
        await using PessoasApiFactory factory = new();

        Pessoa maria = Nova("Maria Souza");
        HttpClient client = await factory.SemearAsync(
            maria,
            Nova("João Guedes", convidadoPor: maria.Id));

        string corpo = await BuscarAsync(client, "joão gue");

        Assert.Contains(@"""nome"":""João Guedes""", corpo, StringComparison.Ordinal);
        Assert.Contains(@"""situacao"":""Visitante""", corpo, StringComparison.Ordinal);
        Assert.Contains(@"""desde"":""2024-03-12""", corpo, StringComparison.Ordinal);
        Assert.Contains(@"""convidadoPor"":{", corpo, StringComparison.Ordinal);
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
                DataNascimento = new DateOnly(1990, 3, 12),
                Endereco = new Endereco(null, null, null, null, "Boa Viagem", null, null),
            });

        string corpo = await BuscarAsync(client, "joão");

        Assert.DoesNotContain("999998888", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("joao@exemplo.com", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("1990-03-12", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("Boa Viagem", corpo, StringComparison.Ordinal);
    }

    private static Pessoa Nova(string nome, PessoaId? convidadoPor = null)
    {
        Pessoa pessoa = new(new PessoaId(Guid.CreateVersion7()), nome)
        {
            ConvidadoPorId = convidadoPor,
        };

        pessoa.SucederVinculo(Situacao.Visitante, Chegada, null, Chegada);

        return pessoa;
    }

    private static async Task<string> BuscarAsync(HttpClient client, string termo)
    {
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"/api/pessoas/search?q={Uri.EscapeDataString(termo)}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }
}
