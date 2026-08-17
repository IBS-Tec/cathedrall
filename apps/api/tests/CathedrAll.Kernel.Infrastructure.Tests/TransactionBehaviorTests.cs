using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Kernel.Infrastructure.Tests;

public sealed class TransactionBehaviorTests
{
    [Fact]
    public async Task Deve_salvar_e_confirmar_quando_o_comando_termina_em_sucesso()
    {
        await using SqliteConnection connection = await Scenario.OpenAsync();

        using ServiceProvider provider = Scenario.Build(connection, services =>
            services.AddScoped<IRequestHandler<FakeWriteCommand, Result<string>>, WritingHandler>());

        Result<string> response = await Scenario.SendAsync(provider, "confirmado");

        Assert.True(response.IsSuccess);
        Assert.Equal(WritingHandler.Response, response.Value);
        Assert.Equal(1, await Scenario.CountRowsAsync(connection));
    }

    [Fact]
    public async Task Handler_nao_precisa_chamar_SaveChanges()
    {
        await using SqliteConnection connection = await Scenario.OpenAsync();

        using ServiceProvider provider = Scenario.Build(connection, services =>
            services.AddScoped<IRequestHandler<FakeWriteCommand, Result<string>>, WritingHandler>());

        await Scenario.SendAsync(provider, "sem save no handler");

        Assert.Equal(1, await Scenario.CountRowsAsync(connection));
    }

    [Fact]
    public async Task Deve_desfazer_a_escrita_quando_o_resultado_e_falha()
    {
        await using SqliteConnection connection = await Scenario.OpenAsync();

        using ServiceProvider provider = Scenario.Build(connection, services =>
            services.AddScoped<IRequestHandler<FakeWriteCommand, Result<string>>, WritingRejectingHandler>());

        Result<string> response = await Scenario.SendAsync(provider, "rejeitado");

        Assert.True(response.IsFailure);
        Assert.Equal(WritingRejectingHandler.Rejection, response.Error);
        Assert.Equal(0, await Scenario.CountRowsAsync(connection));
    }

    [Fact]
    public async Task Deve_desfazer_o_SaveChanges_que_o_proprio_handler_fez()
    {
        await using SqliteConnection connection = await Scenario.OpenAsync();

        using ServiceProvider provider = Scenario.Build(connection, services =>
            services.AddScoped<IRequestHandler<FakeWriteCommand, Result<string>>, EagerlySavingHandler>());

        Result<string> response = await Scenario.SendAsync(provider, "salvo e rejeitado");

        Assert.True(response.IsFailure);
        Assert.Equal(0, await Scenario.CountRowsAsync(connection));
    }

    [Fact]
    public async Task Deve_desfazer_o_SaveChanges_do_handler_quando_ele_lanca_depois()
    {
        await using SqliteConnection connection = await Scenario.OpenAsync();

        using ServiceProvider provider = Scenario.Build(connection, services =>
            services.AddScoped<IRequestHandler<FakeWriteCommand, Result<string>>, EagerlySavingThrowingHandler>());

        await Assert.ThrowsAsync<TimeoutException>(() => Scenario.SendAsync(provider, "salvo e explodiu"));

        Assert.Equal(0, await Scenario.CountRowsAsync(connection));
    }

    [Fact]
    public async Task Deve_desfazer_a_escrita_e_propagar_quando_o_handler_lanca()
    {
        await using SqliteConnection connection = await Scenario.OpenAsync();

        using ServiceProvider provider = Scenario.Build(connection, services =>
            services.AddScoped<IRequestHandler<FakeWriteCommand, Result<string>>, WritingThrowingHandler>());

        TimeoutException exception = await Assert.ThrowsAsync<TimeoutException>(
            () => Scenario.SendAsync(provider, "exceção"));

        Assert.Equal(WritingThrowingHandler.FailureMessage, exception.Message);
        Assert.Equal(0, await Scenario.CountRowsAsync(connection));
    }

    [Fact]
    public async Task Dois_aneis_sobre_o_mesmo_contexto_devem_falhar_alto()
    {
        await using SqliteConnection connection = await Scenario.OpenAsync();

        using ServiceProvider provider = Scenario.Build(connection, services =>
        {
            services.AddScoped<IRequestHandler<FakeWriteCommand, Result<string>>, WritingHandler>();
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(SecondFakeTransactionBehavior<,>));
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Scenario.SendAsync(provider, "dois anéis"));

        Assert.Equal(0, await Scenario.CountRowsAsync(connection));
    }
}
