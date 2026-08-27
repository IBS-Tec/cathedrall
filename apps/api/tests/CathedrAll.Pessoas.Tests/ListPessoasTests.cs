using CathedrAll.Pessoas.Application;
using CathedrAll.Pessoas.Domain;
using CathedrAll.Pessoas.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Pessoas.Tests;

public sealed class ListPessoasTests
{
    private static readonly DateOnly Chegada = new(2024, 3, 12);
    private static readonly DateOnly Apresentacao = new(2024, 9, 15);
    private static readonly DateOnly Afastamento = new(2025, 2, 1);

    [Fact]
    public async Task Sem_filtro_deve_devolver_a_primeira_pagina_e_nunca_a_lista_inteira()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        await SemearAsync(provider, [.. Enumerable.Range(1, 60).Select(n => Nova($"Maria {n:00}"))]);

        ListPessoasResponse resposta = await ListarAsync(provider);

        Assert.Equal(25, resposta.Items.Count);
        Assert.Equal(1, resposta.Page);
        Assert.Equal(25, resposta.Size);
        Assert.Equal(60, resposta.Total);
    }

    [Fact]
    public async Task Size_deve_ter_teto_imposto_pelo_servidor()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        await SemearAsync(provider, [.. Enumerable.Range(1, 60).Select(n => Nova($"Maria {n:00}"))]);

        ListPessoasResponse resposta = await ListarAsync(provider, size: 999);

        Assert.Equal(50, resposta.Items.Count);
        Assert.Equal(50, resposta.Size);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, -1)]
    public async Task Page_e_size_invalidos_devem_cair_no_menor_valor_valido(int page, int size)
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        await SemearAsync(provider, Nova("João Guedes"), Nova("Maria Souza"));

        ListPessoasResponse resposta = await ListarAsync(provider, page: page, size: size);

        Assert.Equal(1, resposta.Page);
        Assert.Equal(1, resposta.Size);
        Assert.Single(resposta.Items);
    }

    [Fact]
    public async Task Paginas_nao_devem_repetir_nem_pular_registro_de_nome_igual()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        await SemearAsync(provider, [.. Enumerable.Range(1, 6).Select(_ => Nova("Maria Souza"))]);

        Guid[] paginadas =
        [
            .. (await ListarAsync(provider, page: 1, size: 2)).Items.Select(p => p.Id),
            .. (await ListarAsync(provider, page: 2, size: 2)).Items.Select(p => p.Id),
            .. (await ListarAsync(provider, page: 3, size: 2)).Items.Select(p => p.Id),
        ];

        Assert.Equal(6, paginadas.Distinct().Count());
    }

    [Fact]
    public async Task Filtro_por_situacao_deve_olhar_so_o_vinculo_vigente()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        Pessoa membro = Nova("João Guedes");
        membro.SucederVinculo(Situacao.Membro, Apresentacao, null, Apresentacao);

        Pessoa afastada = Nova("Maria Souza");
        afastada.SucederVinculo(Situacao.Membro, Apresentacao, null, Apresentacao);
        afastada.SucederVinculo(Situacao.Afastado, Afastamento, "mudou de cidade", Afastamento);

        await SemearAsync(provider, membro, afastada);

        ListPessoasResponse resposta = await ListarAsync(provider, situacao: Situacao.Membro);

        Assert.Equal("João Guedes", Assert.Single(resposta.Items).Nome);
        Assert.Equal(1, resposta.Total);
    }

    [Theory]
    [InlineData("grotao")]
    [InlineData("GROTÃO")]
    [InlineData("  Grotão  ")]
    public async Task Filtro_por_bairro_deve_casar_contra_o_valor_normalizado(string digitado)
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        await SemearAsync(
            provider,
            Nova("João Guedes", bairro: "Grotão"),
            Nova("Maria Souza", bairro: "Casa Amarela"));

        ListPessoasResponse resposta = await ListarAsync(provider, bairro: digitado);

        Assert.Equal("João Guedes", Assert.Single(resposta.Items).Nome);
    }

    [Fact]
    public async Task Bairro_deve_voltar_como_foi_gravado_e_nao_normalizado()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        await SemearAsync(provider, Nova("João Guedes", bairro: "Grotão"));

        Assert.Equal("Grotão", Assert.Single((await ListarAsync(provider)).Items).Bairro);
    }

    [Fact]
    public async Task Pessoa_sem_endereco_deve_ter_bairro_nulo()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        await SemearAsync(provider, Nova("João Guedes"));

        Assert.Null(Assert.Single((await ListarAsync(provider)).Items).Bairro);
    }

    [Theory]
    [InlineData("joão gue")]
    [InlineData("joao")]
    [InlineData("guedes joão")]
    [InlineData("gue")]
    public async Task Filtro_por_nome_deve_casar_por_token_como_a_busca(string termo)
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        await SemearAsync(provider, Nova("João Guedes"), Nova("Maria Souza"));

        ListPessoasResponse resposta = await ListarAsync(provider, term: termo);

        Assert.Equal("João Guedes", Assert.Single(resposta.Items).Nome);
    }

    [Fact]
    public async Task Registro_absorvido_por_fusao_nao_deve_aparecer_nem_ser_contado()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        Pessoa sobrevivente = Nova("João Guedes");

        await SemearAsync(provider, sobrevivente, Nova("João Guedes", fundidaEm: sobrevivente.Id));

        ListPessoasResponse resposta = await ListarAsync(provider);

        Assert.Equal(sobrevivente.Id.Value, Assert.Single(resposta.Items).Id);
        Assert.Equal(1, resposta.Total);
    }

    [Fact]
    public async Task Os_tres_filtros_devem_valer_ao_mesmo_tempo()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        Pessoa alvo = Nova("João Guedes", bairro: "Grotão");
        alvo.SucederVinculo(Situacao.Membro, Apresentacao, null, Apresentacao);

        Pessoa outroBairro = Nova("João Guedes", bairro: "Casa Amarela");
        outroBairro.SucederVinculo(Situacao.Membro, Apresentacao, null, Apresentacao);

        await SemearAsync(
            provider,
            alvo,
            outroBairro,
            Nova("João Guedes", bairro: "Grotão"),
            Nova("Maria Souza", bairro: "Grotão"));

        ListPessoasResponse resposta = await ListarAsync(
            provider,
            term: "guedes",
            situacao: Situacao.Membro,
            bairro: "grotao");

        Assert.Equal(alvo.Id.Value, Assert.Single(resposta.Items).Id);
    }

    [Fact]
    public async Task Total_deve_contar_o_filtro_inteiro_e_nao_a_pagina()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        await SemearAsync(
            provider,
            [
                .. Enumerable.Range(1, 30).Select(n => Nova($"Maria {n:00}", bairro: "Grotão")),
                .. Enumerable.Range(1, 10).Select(n => Nova($"Maria {n:00}", bairro: "Casa Amarela")),
            ]);

        ListPessoasResponse resposta = await ListarAsync(provider, bairro: "grotao", size: 5);

        Assert.Equal(5, resposta.Items.Count);
        Assert.Equal(30, resposta.Total);
    }

    [Fact]
    public async Task Situacao_e_desde_devem_vir_do_vinculo_vigente()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        Pessoa pessoa = Nova("João Guedes");
        pessoa.SucederVinculo(Situacao.Membro, Apresentacao, null, Apresentacao);

        await SemearAsync(provider, pessoa);

        PessoaDaLista item = Assert.Single((await ListarAsync(provider)).Items);

        Assert.Equal(Situacao.Membro, item.Situacao);
        Assert.Equal(Apresentacao, item.Desde);
    }

    private static Pessoa Nova(string nome, string? bairro = null, PessoaId? fundidaEm = null)
    {
        Pessoa pessoa = new(new PessoaId(Guid.CreateVersion7()), nome)
        {
            FundidaEmId = fundidaEm,
            Endereco = bairro is null
                ? null
                : new Endereco(null, null, null, null, bairro, null, null),
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

    private static async Task<ListPessoasResponse> ListarAsync(
        ServiceProvider provider,
        string? term = null,
        Situacao? situacao = null,
        string? bairro = null,
        int? page = null,
        int? size = null)
    {
        using IServiceScope scope = provider.CreateScope();
        PessoasDbContext context = scope.ServiceProvider.GetRequiredService<PessoasDbContext>();

        return await new ListPessoasHandler(context).HandleAsync(
            new ListPessoasQuery(term, situacao, bairro, page, size),
            TestContext.Current.CancellationToken);
    }
}
