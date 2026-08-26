using CathedrAll.Pessoas.Application;
using CathedrAll.Pessoas.Domain;
using CathedrAll.Pessoas.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Pessoas.Tests;

public sealed class SearchPessoasTests
{
    private static readonly DateOnly Chegada = new(2024, 3, 12);
    private static readonly DateOnly Apresentacao = new(2024, 9, 15);

    [Theory]
    [InlineData("joão gue")]
    [InlineData("joao")]
    [InlineData("JOAO GUEDES")]
    [InlineData("guedes joão")]
    [InlineData("gue")]
    public async Task Deve_achar_por_token_parcial_sem_acento_e_em_qualquer_ordem(string termo)
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        await SemearAsync(provider, Nova("João Guedes"), Nova("Maria Souza"));

        SearchPessoasResponse resposta = await BuscarAsync(provider, termo);

        Assert.Equal("João Guedes", Assert.Single(resposta.Results).Nome);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("zuza")]
    public async Task Termo_vazio_ou_sem_correspondencia_deve_devolver_lista_vazia(string termo)
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        await SemearAsync(provider, Nova("João Guedes"));

        SearchPessoasResponse resposta = await BuscarAsync(provider, termo);

        Assert.Empty(resposta.Results);
    }

    [Fact]
    public async Task Nunca_deve_devolver_mais_de_dez_resultados()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        await SemearAsync(
            provider,
            [.. Enumerable.Range(1, 12).Select(numero => Nova($"Maria {numero:00}"))]);

        SearchPessoasResponse resposta = await BuscarAsync(provider, "maria");

        Assert.Equal(10, resposta.Results.Count);
    }

    [Fact]
    public async Task Ordem_deve_ignorar_acento_e_nao_depender_da_ordem_de_insercao()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        await SemearAsync(provider, Nova("Maria Zuza"), Nova("Maria Ávila"), Nova("Maria Brito"));

        SearchPessoasResponse resposta = await BuscarAsync(provider, "maria");

        Assert.Equal(
            ["Maria Ávila", "Maria Brito", "Maria Zuza"],
            resposta.Results.Select(encontrada => encontrada.Nome));
    }

    [Fact]
    public async Task Desde_deve_ser_o_inicio_do_vinculo_vigente()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        Pessoa pessoa = Nova("João Guedes");
        pessoa.SucederVinculo(Situacao.Membro, Apresentacao, null, Apresentacao);

        await SemearAsync(provider, pessoa);

        PessoaEncontrada encontrada = Assert.Single((await BuscarAsync(provider, "joão")).Results);

        Assert.Equal(Situacao.Membro, encontrada.Situacao);
        Assert.Equal(Apresentacao, encontrada.Desde);
    }

    [Fact]
    public async Task ConvidadoPor_deve_trazer_id_e_nome()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        Pessoa maria = Nova("Maria Souza");

        await SemearAsync(provider, maria, Nova("João Guedes", convidadoPor: maria.Id));

        PessoaEncontrada encontrada = Assert.Single((await BuscarAsync(provider, "guedes")).Results);

        Assert.NotNull(encontrada.ConvidadoPor);

        Assert.Equal(maria.Id.Value, encontrada.ConvidadoPor.Id);
        Assert.Equal("Maria Souza", encontrada.ConvidadoPor.Nome);
    }

    [Fact]
    public async Task ConvidadoPor_deve_ser_nulo_quando_ninguem_convidou()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        await SemearAsync(provider, Nova("João Guedes"));

        PessoaEncontrada encontrada = Assert.Single((await BuscarAsync(provider, "joão")).Results);

        Assert.Null(encontrada.ConvidadoPor);
    }

    [Fact]
    public async Task Registro_absorvido_deve_resolver_para_o_sobrevivente()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        Pessoa sobrevivente = Nova("João Guedes");

        await SemearAsync(provider, sobrevivente, Nova("João Guedes", fundidaEm: sobrevivente.Id));

        PessoaEncontrada encontrada = Assert.Single((await BuscarAsync(provider, "joão")).Results);

        Assert.Equal(sobrevivente.Id.Value, encontrada.Id);
    }

    [Fact]
    public async Task Pessoa_sem_vinculo_nao_deve_derrubar_a_busca()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        await SemearAsync(provider, new Pessoa(new PessoaId(Guid.CreateVersion7()), "João Guedes"));

        SearchPessoasResponse resposta = await BuscarAsync(provider, "joão");

        Assert.Single(resposta.Results);
    }

    private static Pessoa Nova(
        string nome,
        PessoaId? convidadoPor = null,
        PessoaId? fundidaEm = null)
    {
        Pessoa pessoa = new(new PessoaId(Guid.CreateVersion7()), nome)
        {
            ConvidadoPorId = convidadoPor,
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

    private static async Task<SearchPessoasResponse> BuscarAsync(
        ServiceProvider provider,
        string termo)
    {
        using IServiceScope scope = provider.CreateScope();
        PessoasDbContext context = scope.ServiceProvider.GetRequiredService<PessoasDbContext>();

        return await new SearchPessoasHandler(context)
            .HandleAsync(new SearchPessoasQuery(termo), TestContext.Current.CancellationToken);
    }
}
