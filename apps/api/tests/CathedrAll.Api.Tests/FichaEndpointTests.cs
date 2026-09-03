using System.Net;
using System.Text.Json;
using CathedrAll.Kernel.Application;
using CathedrAll.Pessoas.Domain;

namespace CathedrAll.Api.Tests;

public sealed class FichaEndpointTests
{
    private static readonly DateOnly Chegada = new(2024, 3, 12);
    private static readonly DateOnly Apresentacao = new(2024, 9, 15);
    private static readonly DateOnly Afastamento = new(2025, 6, 1);

    [Fact]
    public async Task Id_inexistente_deve_responder_404_no_formato_do_ADR_0014()
    {
        await using PessoasApiFactory factory = new();
        HttpClient client = await factory.SemearAsync(Nova("João Guedes"));

        HttpResponseMessage response = await client.GetAsync(
            new Uri($"/api/pessoas/{Guid.CreateVersion7()}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        string corpo = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains(@"""code"":""Pessoa.NotFound""", corpo, StringComparison.Ordinal);
        Assert.Contains(@"""traceId"":", corpo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Pessoa_fundida_deve_responder_200_apontando_o_sobrevivente()
    {
        await using PessoasApiFactory factory = new();

        Pessoa sobrevivente = Nova("Maria Souza");
        Pessoa absorvida = Nova("Maria S.", sobrevivente.Id);

        HttpClient client = await factory.SemearAsync(sobrevivente, absorvida);

        // LerFichaAsync exige 200: registro absorvido nunca responde 404 (RN-24).
        string corpo = await LerFichaAsync(client, absorvida);

        using var documento = JsonDocument.Parse(corpo);
        JsonElement ficha = documento.RootElement;
        JsonElement fundidaEm = ficha.GetProperty("fundidaEm");

        // A ficha é a da absorvida, não a do sobrevivente — a rota não redireciona.
        Assert.Equal(absorvida.Id.Value, ficha.GetProperty("id").GetGuid());
        Assert.Equal("Maria S.", Texto(ficha, "nome"));

        Assert.Equal(sobrevivente.Id.Value, fundidaEm.GetProperty("id").GetGuid());
        Assert.Equal("Maria Souza", Texto(fundidaEm, "nome"));
    }

    [Fact]
    public async Task Pessoa_anonimizada_deve_vir_marcada_com_o_historico_preservado()
    {
        await using PessoasApiFactory factory = new();
        Pessoa pessoa = ComHistorico(anonimizada: true);
        HttpClient client = await factory.SemearAsync(pessoa);

        string corpo = await LerFichaAsync(client, pessoa);

        using var documento = JsonDocument.Parse(corpo);

        Assert.True(documento.RootElement.GetProperty("anonimizada").GetBoolean());

        // A RN-16 preserva o histórico, e a ficha não esconde nada por causa da marca: quem
        // apaga dado pessoal é o Anonimizar(), gravando por cima. Se alguém puser um
        // `anonimizada ? null : …` na projeção, é aqui que quebra.
        Assert.Equal(3, Vinculos(corpo).Length);
    }

    [Fact]
    public async Task Ficha_comum_nao_deve_carregar_marca_de_estado_do_registro()
    {
        await using PessoasApiFactory factory = new();
        Pessoa pessoa = Nova("João Guedes");
        HttpClient client = await factory.SemearAsync(pessoa);

        string corpo = await LerFichaAsync(client, pessoa);

        using var documento = JsonDocument.Parse(corpo);
        JsonElement ficha = documento.RootElement;

        Assert.Equal(JsonValueKind.Null, ficha.GetProperty("fundidaEm").ValueKind);
        Assert.False(ficha.GetProperty("anonimizada").GetBoolean());
    }

    [Fact]
    public async Task Vinculos_devem_vir_em_ordem_cronologica()
    {
        await using PessoasApiFactory factory = new();
        Pessoa pessoa = ComHistorico();
        HttpClient client = await factory.SemearAsync(pessoa);

        string corpo = await LerFichaAsync(client, pessoa);

        string?[] situacoes = [.. Vinculos(corpo).Select(vinculo => Texto(vinculo, "situacao"))];

        string?[] esperado = ["Visitante", "Membro", "Afastado"];

        Assert.Equal(esperado, situacoes);
    }

    [Fact]
    public async Task Secretaria_deve_ler_o_motivo_do_afastamento()
    {
        await using PessoasApiFactory factory = new() { Papel = Papel.Secretaria };
        Pessoa pessoa = ComHistorico();
        HttpClient client = await factory.SemearAsync(pessoa);

        string corpo = await LerFichaAsync(client, pessoa);

        Assert.Equal("Mudou de cidade", Texto(VinculoAfastado(corpo), "motivo"));
    }

    [Fact]
    public async Task Recepcao_nao_deve_ler_o_motivo_do_afastamento()
    {
        await using PessoasApiFactory factory = new() { Papel = Papel.Recepcao };
        Pessoa pessoa = ComHistorico();
        HttpClient client = await factory.SemearAsync(pessoa);

        string corpo = await LerFichaAsync(client, pessoa);

        Assert.DoesNotContain("Mudou de cidade", corpo, StringComparison.Ordinal);
        Assert.Null(Texto(VinculoAfastado(corpo), "motivo"));
    }

    // O vínculo é lido do array, e não por substring no corpo: `situacao` aparece duas vezes
    // no JSON — a mastigada do nível de cima e a de cada vínculo — e casar por texto acha a
    // errada.
    private static JsonElement[] Vinculos(string corpo)
    {
        using var documento = JsonDocument.Parse(corpo);

        return [.. documento.RootElement
            .GetProperty("vinculos")
            .EnumerateArray()
            .Select(vinculo => vinculo.Clone())];
    }

    private static JsonElement VinculoAfastado(string corpo) =>
        Assert.Single(Vinculos(corpo), vinculo => Texto(vinculo, "situacao") == "Afastado");

    private static string? Texto(JsonElement elemento, string campo) =>
        elemento.GetProperty(campo).GetString();

    private static async Task<string> LerFichaAsync(HttpClient client, Pessoa pessoa)
    {
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"/api/pessoas/{pessoa.Id.Value}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    private static Pessoa ComHistorico(bool anonimizada = false)
    {
        Pessoa pessoa = Nova("João Guedes", anonimizada: anonimizada);

        pessoa.SucederVinculo(Situacao.Membro, Apresentacao, null, Apresentacao);
        pessoa.SucederVinculo(Situacao.Afastado, Afastamento, "Mudou de cidade", Afastamento);

        return pessoa;
    }

    private static Pessoa Nova(string nome, PessoaId? fundidaEm = null, bool anonimizada = false)
    {
        Pessoa pessoa = new(new PessoaId(Guid.CreateVersion7()), nome)
        {
            Endereco = new Endereco("52000000", "Rua das Flores", "123-A", null, "Grotão", "Recife", "PE"),
            FundidaEmId = fundidaEm,
            Anonimizada = anonimizada,
        };

        pessoa.SucederVinculo(Situacao.Visitante, Chegada, null, Chegada);

        return pessoa;
    }
}
