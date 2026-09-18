using System.Net;
using System.Text.Json;
using CathedrAll.Pessoas.Domain;

namespace CathedrAll.Api.Tests;

public sealed class AnonimizacaoEndpointTests
{
    private static readonly DateOnly Chegada = new(2024, 3, 12);
    private static readonly DateOnly Apresentacao = new(2024, 9, 15);

    [Fact]
    public async Task Anonimizacao_deve_responder_204_sem_corpo()
    {
        await using PessoasApiFactory factory = new();
        Pessoa pessoa = Nova("João Guedes");
        HttpClient client = await factory.SemearAsync(pessoa);

        HttpResponseMessage response = await AnonimizarAsync(client, pessoa);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Pessoa_deve_sumir_da_busca_e_da_lista_com_o_id_ainda_resolvendo()
    {
        await using PessoasApiFactory factory = new();
        Pessoa pessoa = ComHistorico("João Guedes");
        HttpClient client = await factory.SemearAsync(pessoa);

        await AnonimizarAsync(client, pessoa);

        Assert.DoesNotContain(
            "João Guedes",
            await GetAsync(client, "/api/pessoas/search?q=joão"),
            StringComparison.Ordinal);

        Assert.Contains(
            @"""total"":0",
            await GetAsync(client, "/api/pessoas"),
            StringComparison.Ordinal);

        // A ficha continua respondendo 200: o Id é o que os outros módulos guardam sem chave
        // estrangeira, e some da busca não é some do sistema (RN-15).
        string ficha = await GetAsync(client, $"/api/pessoas/{pessoa.Id.Value}");

        using var documento = JsonDocument.Parse(ficha);
        JsonElement corpo = documento.RootElement;

        Assert.Equal(pessoa.Id.Value, corpo.GetProperty("id").GetGuid());
        Assert.True(corpo.GetProperty("anonimizada").GetBoolean());
        Assert.DoesNotContain("João Guedes", ficha, StringComparison.Ordinal);
        Assert.Equal(2, corpo.GetProperty("vinculos").GetArrayLength());
    }

    [Fact]
    public async Task Id_inexistente_deve_responder_404_no_formato_do_ADR_0014()
    {
        await using PessoasApiFactory factory = new();
        HttpClient client = await factory.SemearAsync(Nova("João Guedes"));

        HttpResponseMessage response = await client.PostAsync(
            new Uri($"/api/pessoas/{Guid.CreateVersion7()}/anonimizacao", UriKind.Relative),
            content: null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        string corpo = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains(@"""code"":""Pessoa.NotFound""", corpo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Segunda_anonimizacao_deve_responder_409_com_Pessoa_Anonimizada()
    {
        await using PessoasApiFactory factory = new();
        Pessoa pessoa = Nova("João Guedes");
        HttpClient client = await factory.SemearAsync(pessoa);

        await AnonimizarAsync(client, pessoa);

        HttpResponseMessage response = await AnonimizarAsync(client, pessoa);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        string corpo = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains(@"""code"":""Pessoa.Anonimizada""", corpo, StringComparison.Ordinal);
    }

    private static async Task<HttpResponseMessage> AnonimizarAsync(HttpClient client, Pessoa pessoa) =>
        await client.PostAsync(
            new Uri($"/api/pessoas/{pessoa.Id.Value}/anonimizacao", UriKind.Relative),
            content: null,
            TestContext.Current.CancellationToken);

    private static async Task<string> GetAsync(HttpClient client, string rota)
    {
        HttpResponseMessage response = await client.GetAsync(
            new Uri(rota, UriKind.Relative),
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    private static Pessoa ComHistorico(string nome)
    {
        Pessoa pessoa = Nova(nome);
        pessoa.RegistrarApresentacao(Apresentacao, Apresentacao);

        return pessoa;
    }

    private static Pessoa Nova(string nome) =>
        Pessoa.Cadastrar(
            hoje: Chegada,
            nome: nome,
            convidadoPorId: null,
            celular: new Celular("+5581999998888"),
            email: new Email("joao@exemplo.com"),
            dataNascimento: new DateOnly(1990, 3, 12),
            estadoCivil: EstadoCivil.Casado,
            dataCasamento: new DateOnly(2015, 6, 20),
            profissao: "Eletricista",
            dataBatismo: new DateOnly(2010, 8, 1),
            endereco: new Endereco("52000000", "Rua das Flores", "123-A", "Apto 2", "Grotão", "Recife", "PE")).Value;
}
