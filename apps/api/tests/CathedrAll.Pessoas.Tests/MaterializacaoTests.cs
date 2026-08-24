using CathedrAll.Pessoas.Domain;
using CathedrAll.Pessoas.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Pessoas.Tests;

public sealed class MaterializacaoTests
{
    private static readonly PessoaId Convite = new(Guid.CreateVersion7());

    [Fact]
    public async Task Pessoa_completa_deve_sobreviver_a_ida_e_volta_ao_banco()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        PessoaId id = await Scenario.GravarAsync(provider, Completa());

        using IServiceScope scope = provider.CreateScope();
        PessoasDbContext context = scope.ServiceProvider.GetRequiredService<PessoasDbContext>();

        Pessoa lida = await context.Pessoas.SingleAsync(
            p => p.Id == id,
            TestContext.Current.CancellationToken);

        Assert.Equal("João Guedes", lida.Nome);
        Assert.Equal(Convite, lida.ConvidadoPorId);
        Assert.Null(lida.FundidaEmId);
        Assert.Equal(new Celular("+5581999998888"), lida.Celular);
        Assert.Equal(new Email("joao@exemplo.com"), lida.Email);
        Assert.Equal(new DateOnly(1990, 3, 12), lida.DataNascimento);
        Assert.Equal(EstadoCivil.Casado, lida.EstadoCivil);
        Assert.Equal(new DateOnly(2015, 6, 20), lida.DataCasamento);
        Assert.Equal("Eletricista", lida.Profissao);
        Assert.Equal(new DateOnly(2010, 8, 1), lida.DataBatismo);
        Assert.Equal(
            new Endereco("52000000", "Rua das Flores", "123-A", "Apto 2", "Boa Viagem", "Recife", "PE"),
            lida.Endereco);
    }

    [Fact]
    public async Task Pessoa_so_com_nome_deve_voltar_com_os_demais_campos_nulos()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        PessoaId id = await Scenario.GravarAsync(
            provider,
            new Pessoa(new PessoaId(Guid.CreateVersion7()), "Maria"));

        using IServiceScope scope = provider.CreateScope();
        PessoasDbContext context = scope.ServiceProvider.GetRequiredService<PessoasDbContext>();

        Pessoa lida = await context.Pessoas.SingleAsync(
            p => p.Id == id,
            TestContext.Current.CancellationToken);

        Assert.Equal("Maria", lida.Nome);
        Assert.Null(lida.Endereco);
        Assert.Null(lida.Celular);
        Assert.Null(lida.DataNascimento);
        Assert.Null(lida.ConvidadoPorId);
    }

    [Fact]
    public async Task Vinculos_devem_ser_carregados_na_colecao_sem_setter()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        PessoaId id = new(Guid.CreateVersion7());
        await Scenario.GravarComVinculoAsync(provider, id);

        using IServiceScope scope = provider.CreateScope();
        PessoasDbContext context = scope.ServiceProvider.GetRequiredService<PessoasDbContext>();

        Pessoa lida = await context.Pessoas
            .Include(p => p.Vinculos)
            .SingleAsync(p => p.Id == id, TestContext.Current.CancellationToken);

        VinculoIgreja vinculo = Assert.Single(lida.Vinculos);

        Assert.Equal(id, vinculo.PessoaId);
        Assert.Equal(Situacao.Visitante, vinculo.Situacao);
        Assert.Equal(new DateOnly(2026, 8, 23), vinculo.DataInicio);
        Assert.Null(vinculo.DataFim);
    }

    [Fact]
    public async Task Objetos_de_valor_e_enum_devem_ser_gravados_como_primitivo()
    {
        const string sql = "SELECT celular, email, estado_civil, endereco_bairro FROM pessoas";

        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.Provedor(connection);

        await Scenario.GravarAsync(provider, Completa());

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;

        await using SqliteDataReader reader =
            await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        Assert.True(await reader.ReadAsync(TestContext.Current.CancellationToken));

        Assert.Equal("+5581999998888", reader.GetString(0));
        Assert.Equal("joao@exemplo.com", reader.GetString(1));
        Assert.Equal("Casado", reader.GetString(2));
        Assert.Equal("Boa Viagem", reader.GetString(3));
    }

    private static Pessoa Completa() =>
        new(new PessoaId(Guid.CreateVersion7()), "João Guedes")
        {
            ConvidadoPorId = Convite,
            Celular = new Celular("+5581999998888"),
            Email = new Email("joao@exemplo.com"),
            DataNascimento = new DateOnly(1990, 3, 12),
            EstadoCivil = EstadoCivil.Casado,
            DataCasamento = new DateOnly(2015, 6, 20),
            Profissao = "Eletricista",
            DataBatismo = new DateOnly(2010, 8, 1),
            Endereco = new Endereco("52000000", "Rua das Flores", "123-A", "Apto 2", "Boa Viagem", "Recife", "PE"),
        };
}
