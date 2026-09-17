using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using CathedrAll.Pessoas.Domain;

namespace CathedrAll.Api.Tests;

public sealed class PautaEndpointTests
{
    // 2026-08-23 é domingo, e a semana do culto vai da segunda anterior até ele (seção 6).
    private const string DomingoDoCulto = "2026-08-23";

    private static readonly DateOnly Chegada = new(2024, 3, 12);
    private static readonly DateOnly Culto = new(2026, 8, 23);

    [Fact]
    public async Task Resposta_deve_ter_o_envelope_do_exemplo_da_secao_6()
    {
        await using PessoasApiFactory factory = new();

        Pessoa maria = Membro("Maria Souza", nascimento: new DateOnly(1990, 8, 19));
        Pessoa joao = VisitanteDoDia("João Guedes", convidadoPor: maria);

        HttpClient client = await factory.SemearAsync(maria, joao);

        string corpo = await BuscarAsync(client, DomingoDoCulto);

        Assert.Equal(
            """
            {"visitantes":[{"id":"<id>","nome":"João Guedes","convidadoPor":{"id":"<id>","nome":"Maria Souza"}}],"aniversariantes":[{"id":"<id>","nome":"Maria Souza","tipo":"Nascimento","data":"2026-08-19"}]}
            """,
            SemOsIds(corpo));
    }

    // `date` é obrigatório, e o binding de DateOnly usa cultura invariante: a data escrita
    // como se escreve no Brasil não é aceita na URL.
    [Theory]
    [InlineData("/api/pessoas/pauta")]
    [InlineData("/api/pessoas/pauta?date=")]
    [InlineData("/api/pessoas/pauta?date=domingo")]
    [InlineData("/api/pessoas/pauta?date=23/08/2026")]
    public async Task Data_ausente_ou_ilegivel_deve_responder_400(string rota)
    {
        await using PessoasApiFactory factory = new();
        HttpClient client = await factory.SemearAsync(VisitanteDoDia("João Guedes"));

        HttpResponseMessage response = await client.GetAsync(
            new Uri(rota, UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // `pauta` não é um Guid, então `/{id:guid}` não a captura. É a restrição de rota da
    // seção 6 fazendo o trabalho dela — se alguém tirá-la, é aqui que quebra.
    [Fact]
    public async Task A_rota_nao_deve_ser_confundida_com_a_ficha_de_uma_pessoa()
    {
        await using PessoasApiFactory factory = new();
        HttpClient client = await factory.SemearAsync(VisitanteDoDia("João Guedes"));

        HttpResponseMessage response = await client.GetAsync(
            new Uri($"/api/pessoas/pauta?date={DomingoDoCulto}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Contains(
            "João Guedes",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Domingo_sem_visitante_deve_responder_200_com_lista_vazia()
    {
        await using PessoasApiFactory factory = new();
        HttpClient client = await factory.SemearAsync(
            Membro("Maria Souza", nascimento: new DateOnly(1990, 8, 19)));

        string corpo = await BuscarAsync(client, DomingoDoCulto);

        using var documento = JsonDocument.Parse(corpo);

        Assert.Empty(documento.RootElement.GetProperty("visitantes").EnumerateArray());
        Assert.NotEmpty(documento.RootElement.GetProperty("aniversariantes").EnumerateArray());
    }

    // O campo tem que existir e vir nulo, não sumir do JSON: o cliente gerado da invariante 5
    // se apoia no schema, e campo ausente e campo nulo não são a mesma coisa em OpenAPI.
    [Fact]
    public async Task Visitante_sem_convite_deve_trazer_convidado_por_nulo()
    {
        await using PessoasApiFactory factory = new();
        HttpClient client = await factory.SemearAsync(VisitanteDoDia("João Guedes"));

        string corpo = await BuscarAsync(client, DomingoDoCulto);

        using var documento = JsonDocument.Parse(corpo);

        JsonElement visitante = Assert.Single(
            [.. documento.RootElement.GetProperty("visitantes").EnumerateArray()]);

        Assert.Equal(JsonValueKind.Null, visitante.GetProperty("convidadoPor").ValueKind);
    }

    // A pauta é a única resposta do módulo que junta duas projeções, e o dirigente é o papel
    // mais amplo a abrir esta tela (seção 7). O ano de nascimento entra na lista porque a
    // RN-25 compara só dia e mês: o que sai é a data deste ano, nunca a de 1990.
    [Fact]
    public async Task Corpo_nao_deve_carregar_dado_pessoal_alem_do_nome()
    {
        await using PessoasApiFactory factory = new();

        HttpClient client = await factory.SemearAsync(
            Pessoa.Cadastrar(
                hoje: Culto,
                nome: "João Guedes",
                convidadoPorId: null,
                celular: new Celular("+5581999998888"),
                email: new Email("joao@exemplo.com"),
                dataNascimento: new DateOnly(1990, 8, 19),
                estadoCivil: EstadoCivil.Casado,
                dataCasamento: null,
                profissao: "Eletricista",
                dataBatismo: null,
                endereco: new Endereco(null, null, null, null, "Boa Viagem", null, null)).Value);

        string corpo = await BuscarAsync(client, DomingoDoCulto);

        Assert.Contains("João Guedes", corpo, StringComparison.Ordinal);
        Assert.Contains("2026-08-19", corpo, StringComparison.Ordinal);

        Assert.DoesNotContain("999998888", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("joao@exemplo.com", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("1990", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("Boa Viagem", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("Eletricista", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("Casado", corpo, StringComparison.Ordinal);
    }

    private static Pessoa VisitanteDoDia(string nome, Pessoa? convidadoPor = null) =>
        Cadastrar(nome, Culto, convidadoPor?.Id, null, comoMembro: false);

    private static Pessoa Membro(string nome, DateOnly? nascimento = null) =>
        Cadastrar(nome, Chegada, null, nascimento, comoMembro: true);

    private static Pessoa Cadastrar(
        string nome,
        DateOnly chegada,
        PessoaId? convidadoPorId,
        DateOnly? nascimento,
        bool comoMembro) =>
        Pessoa.Cadastrar(
            hoje: chegada,
            nome: nome,
            convidadoPorId: convidadoPorId,
            celular: null,
            email: null,
            dataNascimento: nascimento,
            estadoCivil: null,
            dataCasamento: null,
            profissao: null,
            dataBatismo: null,
            endereco: null,
            comoMembro: comoMembro).Value;

    private static string SemOsIds(string corpo) =>
        Regex.Replace(
            corpo,
            @"""id"":""[0-9a-f-]{36}""",
            @"""id"":""<id>""");

    private static async Task<string> BuscarAsync(HttpClient client, string date)
    {
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"/api/pessoas/pauta?date={date}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }
}
