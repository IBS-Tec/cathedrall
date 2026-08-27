using System.Net;
using CathedrAll.Pessoas.Domain;

namespace CathedrAll.Api.Tests;

public sealed class ListEndpointTests
{
    private static readonly DateOnly Chegada = new(2024, 3, 12);
    private static readonly DateOnly Apresentacao = new(2024, 9, 15);

    [Theory]
    [InlineData("/api/pessoas")]
    [InlineData("/api/pessoas/")]
    public async Task A_lista_deve_responder_com_e_sem_barra_final(string rota)
    {
        await using PessoasApiFactory factory = new();
        HttpClient client = await factory.SemearAsync(Nova("João Guedes"));

        HttpResponseMessage response = await client.GetAsync(
            new Uri(rota, UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Resposta_deve_ter_o_envelope_do_exemplo_da_secao_6()
    {
        await using PessoasApiFactory factory = new();
        HttpClient client = await factory.SemearAsync(Nova("João Guedes", bairro: "Grotão"));

        string corpo = await ListarAsync(client, string.Empty);

        Assert.Contains(@"""items"":[", corpo, StringComparison.Ordinal);
        Assert.Contains(@"""nome"":""João Guedes""", corpo, StringComparison.Ordinal);
        Assert.Contains(@"""situacao"":""Visitante""", corpo, StringComparison.Ordinal);
        Assert.Contains(@"""desde"":""2024-03-12""", corpo, StringComparison.Ordinal);
        Assert.Contains(@"""bairro"":""Grotão""", corpo, StringComparison.Ordinal);
        Assert.Contains(@"""page"":1", corpo, StringComparison.Ordinal);
        Assert.Contains(@"""size"":25", corpo, StringComparison.Ordinal);
        Assert.Contains(@"""total"":1", corpo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Corpo_nao_deve_carregar_dado_pessoal_alem_do_nome_e_do_bairro()
    {
        await using PessoasApiFactory factory = new();

        HttpClient client = await factory.SemearAsync(
            new Pessoa(new PessoaId(Guid.CreateVersion7()), "João Guedes")
            {
                Celular = new Celular("+5581999998888"),
                Email = new Email("joao@exemplo.com"),
                DataNascimento = new DateOnly(1990, 3, 12),
                Endereco = new Endereco("52000000", "Rua das Flores", "123-A", null, "Grotão", "Recife", "PE"),
            });

        string corpo = await ListarAsync(client, string.Empty);

        Assert.DoesNotContain("999998888", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("joao@exemplo.com", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("1990-03-12", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("Rua das Flores", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("52000000", corpo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Bairro_normalizado_nunca_deve_aparecer_na_resposta()
    {
        await using PessoasApiFactory factory = new();
        HttpClient client = await factory.SemearAsync(Nova("João Guedes", bairro: "Grotão"));

        string corpo = await ListarAsync(client, string.Empty);

        Assert.DoesNotContain("GROTAO", corpo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Os_filtros_devem_chegar_pela_query_string()
    {
        await using PessoasApiFactory factory = new();

        Pessoa membro = Nova("João Guedes", bairro: "Grotão");
        membro.SucederVinculo(Situacao.Membro, Apresentacao, null, Apresentacao);

        HttpClient client = await factory.SemearAsync(
            membro,
            Nova("João Guedes", bairro: "Grotão"),
            Nova("Maria Souza", bairro: "Grotão"));

        string corpo = await ListarAsync(client, "?q=guedes&situacao=Membro&bairro=grotao");

        Assert.Contains(@"""total"":1", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("Maria Souza", corpo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Size_acima_do_teto_deve_voltar_corrigido_na_resposta()
    {
        await using PessoasApiFactory factory = new();
        HttpClient client = await factory.SemearAsync(Nova("João Guedes"));

        string corpo = await ListarAsync(client, "?size=999");

        Assert.Contains(@"""size"":50", corpo, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("?situacao=lixo")]
    [InlineData("?page=abc")]
    [InlineData("?size=abc")]
    public async Task Filtro_ilegivel_deve_responder_400_e_nao_500(string query)
    {
        await using PessoasApiFactory factory = new();
        HttpClient client = await factory.SemearAsync(Nova("João Guedes"));

        HttpResponseMessage response = await client.GetAsync(
            new Uri($"/api/pessoas{query}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        Assert.Contains(
            @"""code"":""Request.Malformed""",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
            StringComparison.Ordinal);
    }

    private static Pessoa Nova(string nome, string? bairro = null)
    {
        Pessoa pessoa = new(new PessoaId(Guid.CreateVersion7()), nome)
        {
            Endereco = bairro is null
                ? null
                : new Endereco(null, null, null, null, bairro, null, null),
        };

        pessoa.SucederVinculo(Situacao.Visitante, Chegada, null, Chegada);

        return pessoa;
    }

    private static async Task<string> ListarAsync(HttpClient client, string query)
    {
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"/api/pessoas{query}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }
}
