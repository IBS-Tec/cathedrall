using CathedrAll.Pessoas.Application;
using CathedrAll.Pessoas.Domain;
using CathedrAll.Pessoas.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Pessoas.Tests;

public sealed class ListAniversariantesTests
{
    private static readonly DateOnly Chegada = new(2024, 3, 12);

    [Fact]
    public async Task Intervalo_que_atravessa_a_virada_do_ano_deve_achar_os_dois_lados()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        await SemearAsync(
            provider,
            Nova("Ana Souza", nascimento: new DateOnly(1990, 12, 30)),
            Nova("Bento Lima", casamento: new DateOnly(2010, 1, 2)),
            Nova("Carla Dias", nascimento: new DateOnly(1985, 6, 15)),
            Nova("Davi Melo"));

        ListAniversariantesResponse resposta = await BuscarAsync(
            provider,
            new DateOnly(2026, 12, 28),
            new DateOnly(2027, 1, 3));

        Assert.Equal(
            ["Ana Souza|Nascimento|2026-12-30", "Bento Lima|Casamento|2027-01-02"],
            Resumir(resposta));
    }

    [Fact]
    public async Task A_mesma_pessoa_deve_aparecer_duas_vezes_com_tipos_diferentes()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        await SemearAsync(
            provider,
            Nova(
                "Ana Souza",
                nascimento: new DateOnly(1990, 8, 25),
                casamento: new DateOnly(2015, 8, 27)));

        ListAniversariantesResponse resposta = await BuscarAsync(
            provider,
            new DateOnly(2026, 8, 23),
            new DateOnly(2026, 8, 29));

        Assert.Equal(
            ["Ana Souza|Nascimento|2026-08-25", "Ana Souza|Casamento|2026-08-27"],
            Resumir(resposta));
    }

    [Fact]
    public async Task Falecido_nao_deve_aparecer() =>
        await QuemSaiuNaoDeveAparecerAsync(Situacao.Falecido);

    [Fact]
    public async Task Transferido_nao_deve_aparecer() =>
        await QuemSaiuNaoDeveAparecerAsync(Situacao.Transferido);

    [Fact]
    public async Task Registro_absorvido_por_fusao_nao_deve_aparecer()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        Pessoa sobrevivente = Nova("Ana Souza", nascimento: new DateOnly(1990, 8, 25));

        await SemearAsync(
            provider,
            sobrevivente,
            Nova("Ana S.", nascimento: new DateOnly(1990, 8, 25), fundidaEm: sobrevivente.Id));

        ListAniversariantesResponse resposta = await BuscarAsync(
            provider,
            new DateOnly(2026, 8, 23),
            new DateOnly(2026, 8, 29));

        Assert.Equal(["Ana Souza|Nascimento|2026-08-25"], Resumir(resposta));
    }

    [Fact]
    public async Task Aniversario_de_29_de_fevereiro_deve_cair_no_dia_28_em_ano_comum()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        await SemearAsync(provider, Nova("Ana Souza", nascimento: new DateOnly(2000, 2, 29)));

        ListAniversariantesResponse resposta = await BuscarAsync(
            provider,
            new DateOnly(2027, 2, 26),
            new DateOnly(2027, 3, 4));

        Assert.Equal(["Ana Souza|Nascimento|2027-02-28"], Resumir(resposta));
    }

    [Fact]
    public async Task Intervalo_grande_demais_deve_ter_teto_imposto_pelo_servidor()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        await SemearAsync(
            provider,
            Nova("Ana Souza", nascimento: new DateOnly(1990, 3, 1)),
            Nova("Bento Lima", nascimento: new DateOnly(1990, 6, 15)));

        ListAniversariantesResponse resposta = await BuscarAsync(
            provider,
            new DateOnly(2027, 2, 26),
            new DateOnly(2028, 2, 26));

        Assert.Equal(["Ana Souza|Nascimento|2027-03-01"], Resumir(resposta));
    }

    private static async Task QuemSaiuNaoDeveAparecerAsync(Situacao situacao)
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        Pessoa saiu = Nova("Ana Souza", nascimento: new DateOnly(1990, 8, 25));
        saiu.SucederVinculo(situacao, new DateOnly(2025, 1, 1), null, new DateOnly(2025, 1, 1));

        await SemearAsync(provider, saiu);

        ListAniversariantesResponse resposta = await BuscarAsync(
            provider,
            new DateOnly(2026, 8, 23),
            new DateOnly(2026, 8, 29));

        Assert.Empty(resposta.Aniversariantes);
    }

    private static string[] Resumir(ListAniversariantesResponse resposta) =>
        [.. resposta.Aniversariantes.Select(
            aniversariante => $"{aniversariante.Nome}|{aniversariante.Tipo}|{aniversariante.Data:yyyy-MM-dd}")];

    private static Pessoa Nova(
        string nome,
        DateOnly? nascimento = null,
        DateOnly? casamento = null,
        PessoaId? fundidaEm = null)
    {
        Pessoa pessoa = new(new PessoaId(Guid.CreateVersion7()), nome)
        {
            DataNascimento = nascimento,
            DataCasamento = casamento,
            FundidaEmId = fundidaEm,
        };

        pessoa.SucederVinculo(Situacao.Visitante, Chegada, null, Chegada);

        return pessoa;
    }

    private static async Task SemearAsync(ServiceProvider provider, params Pessoa[] pessoas)
    {
        using IServiceScope scope = provider.CreateScope();
        PessoasDbContext context = scope.ServiceProvider.GetRequiredService<PessoasDbContext>();

        context.Pessoas.AddRange(pessoas);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<ListAniversariantesResponse> BuscarAsync(
        ServiceProvider provider,
        DateOnly from,
        DateOnly to)
    {
        using IServiceScope scope = provider.CreateScope();
        PessoasDbContext context = scope.ServiceProvider.GetRequiredService<PessoasDbContext>();

        return (await new ListAniversariantesHandler(context).HandleAsync(
            new ListAniversariantesQuery(from, to),
            TestContext.Current.CancellationToken)).Value;
    }
}
