using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;
using CathedrAll.Pessoas.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Pessoas.Tests;

public sealed class AnelDeTransacaoTests
{
    [Fact]
    public async Task Comando_deve_gravar_sem_o_handler_chamar_SaveChanges()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.ProvedorComAnel(connection, services =>
            services.AddScoped<IRequestHandler<FakeWriteCommand, Result<PessoaId>>, WritingHandler>());

        Result<PessoaId> resultado = await Scenario.EnviarComandoAsync(provider, "João Guedes");

        Assert.True(resultado.IsSuccess);
        Assert.Equal(1, await Scenario.ContarPessoasAsync(connection));
    }

    [Fact]
    public async Task Comando_que_falha_depois_de_salvar_nao_deve_deixar_pessoa_gravada()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.ProvedorComAnel(connection, services =>
            services.AddScoped<IRequestHandler<FakeWriteCommand, Result<PessoaId>>, EagerlySavingHandler>());

        Result<PessoaId> resultado = await Scenario.EnviarComandoAsync(provider, "João Guedes");

        Assert.True(resultado.IsFailure);
        Assert.Equal(EagerlySavingHandler.Rejection, resultado.Error);
        Assert.Equal(0, await Scenario.ContarPessoasAsync(connection));
    }

    [Fact]
    public async Task Consulta_nao_deve_passar_pelo_anel()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Scenario.ProvedorComAnel(connection, services =>
            services.AddScoped<IRequestHandler<FakeReadQuery, int>, WritingQueryHandler>());

        await Scenario.EnviarConsultaAsync(provider, "Maria");

        Assert.Equal(0, await Scenario.ContarPessoasAsync(connection));
    }
}
