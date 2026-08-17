using CathedrAll.Kernel.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CathedrAll.Kernel.Application.Tests;

public sealed class LoggingBehaviorTests
{
    private const string Secret = "cpf-do-membro";

    [Fact]
    public async Task Sucesso_deve_registrar_uma_entrada_de_information_com_o_nome_da_requisicao()
    {
        List<LogRecord> records = [];
        List<string> trace = [];

        using ServiceProvider provider = Scenario.Build(services =>
        {
            services.AddSingleton<ILoggerFactory>(new FakeLoggerFactory(records));
            services.AddLoggingBehavior();
            services.AddSingleton<IRequestHandler<FakeRequest, string>>(new FakeHandler(trace));
        });

        using IServiceScope scope = provider.CreateScope();

        await Scenario.SenderFrom(scope)
            .SendAsync<FakeRequest, string>(new FakeRequest("qualquer"), CancellationToken.None);

        LogRecord record = Assert.Single(records);

        Assert.Equal(LogLevel.Information, record.Level);
        Assert.Contains(nameof(FakeRequest), record.Message, StringComparison.Ordinal);
        Assert.Contains("success", record.Message, StringComparison.Ordinal);
        Assert.Null(record.Exception);
    }

    [Fact]
    public async Task Nao_deve_registrar_o_conteudo_da_requisicao()
    {
        List<LogRecord> records = [];
        List<string> trace = [];

        using ServiceProvider provider = Scenario.Build(services =>
        {
            services.AddSingleton<ILoggerFactory>(new FakeLoggerFactory(records));
            services.AddLoggingBehavior();
            services.AddSingleton<IRequestHandler<FakeRequest, string>>(new FakeHandler(trace));
        });

        using IServiceScope scope = provider.CreateScope();

        await Scenario.SenderFrom(scope)
            .SendAsync<FakeRequest, string>(new FakeRequest(Secret), CancellationToken.None);

        AssertNoLeak(records);
    }

    [Fact]
    public async Task Excecao_deve_subir_e_ainda_assim_registrar_o_desfecho()
    {
        List<LogRecord> records = [];

        using ServiceProvider provider = Scenario.Build(services =>
        {
            services.AddSingleton<ILoggerFactory>(new FakeLoggerFactory(records));
            services.AddLoggingBehavior();
            services.AddSingleton<IRequestHandler<FakeRequest, string>>(new ThrowingHandler());
        });

        using IServiceScope scope = provider.CreateScope();

        ISender sender = Scenario.SenderFrom(scope);

        await Assert.ThrowsAsync<TimeoutException>(() =>
            sender.SendAsync<FakeRequest, string>(new FakeRequest(Secret), CancellationToken.None));

        LogRecord record = Assert.Single(records);

        Assert.Equal(LogLevel.Error, record.Level);
        Assert.Contains("exception", record.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(ThrowingHandler.FailureMessage, record.Message, StringComparison.Ordinal);
        Assert.Null(record.Exception);
        AssertNoLeak(records);
    }

    [Fact]
    public async Task Falha_de_negocio_deve_registrar_warning_com_o_codigo_do_erro()
    {
        List<LogRecord> records = [];

        using ServiceProvider provider = Scenario.Build(services =>
        {
            services.AddSingleton<ILoggerFactory>(new FakeLoggerFactory(records));
            services.AddLoggingBehavior();
            services.AddSingleton<IRequestHandler<FakeResultCommand, Result<string>>>(new RejectingHandler());
        });

        using IServiceScope scope = provider.CreateScope();

        Result<string> result = await Scenario.SenderFrom(scope)
            .SendAsync<FakeResultCommand, Result<string>>(new FakeResultCommand(Secret), CancellationToken.None);

        LogRecord record = Assert.Single(records);
        KeyValuePair<string, object?> code = Assert.Single(record.State, field => field.Key == "ErrorCode");

        Assert.True(result.IsFailure);
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.Equal(RejectingHandler.Rejection.Code, code.Value);
        Assert.DoesNotContain(
            RejectingHandler.Rejection.Description,
            record.Message,
            StringComparison.Ordinal);
        AssertNoLeak(records);
    }

    [Fact]
    public async Task Result_bem_sucedido_deve_registrar_information()
    {
        List<LogRecord> records = [];

        using ServiceProvider provider = Scenario.Build(services =>
        {
            services.AddSingleton<ILoggerFactory>(new FakeLoggerFactory(records));
            services.AddLoggingBehavior();
            services.AddSingleton<IRequestHandler<FakeResultCommand, Result<string>>>(new AcceptingHandler());
        });

        using IServiceScope scope = provider.CreateScope();

        await Scenario.SenderFrom(scope)
            .SendAsync<FakeResultCommand, Result<string>>(new FakeResultCommand("qualquer"), CancellationToken.None);

        LogRecord record = Assert.Single(records);

        Assert.Equal(LogLevel.Information, record.Level);
        Assert.DoesNotContain("ErrorCode", record.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Deve_registrar_a_duracao_como_campo_estruturado()
    {
        List<LogRecord> records = [];
        List<string> trace = [];

        using ServiceProvider provider = Scenario.Build(services =>
        {
            services.AddSingleton<ILoggerFactory>(new FakeLoggerFactory(records));
            services.AddLoggingBehavior();
            services.AddSingleton<IRequestHandler<FakeRequest, string>>(new FakeHandler(trace));
        });

        using IServiceScope scope = provider.CreateScope();

        await Scenario.SenderFrom(scope)
            .SendAsync<FakeRequest, string>(new FakeRequest("qualquer"), CancellationToken.None);

        LogRecord record = Assert.Single(records);
        KeyValuePair<string, object?> duration =
            Assert.Single(record.State, field => field.Key == "DurationMs");

        Assert.True(Assert.IsType<double>(duration.Value) >= 0);
    }

    [Fact]
    public async Task Deve_usar_uma_categoria_fixa()
    {
        List<LogRecord> records = [];
        List<string> trace = [];
        using FakeLoggerFactory factory = new(records);

        using ServiceProvider provider = Scenario.Build(services =>
        {
            services.AddSingleton<ILoggerFactory>(factory);
            services.AddLoggingBehavior();
            services.AddSingleton<IRequestHandler<FakeRequest, string>>(new FakeHandler(trace));
        });

        using IServiceScope scope = provider.CreateScope();

        await Scenario.SenderFrom(scope)
            .SendAsync<FakeRequest, string>(new FakeRequest("qualquer"), CancellationToken.None);

        Assert.Equal("CathedrAll.Kernel.Application.Pipeline", Assert.Single(factory.Categories));
    }

    private static void AssertNoLeak(List<LogRecord> records) =>
        Assert.All(records, record =>
        {
            Assert.DoesNotContain(Secret, record.Message, StringComparison.Ordinal);
            Assert.All(record.State, field =>
                Assert.DoesNotContain(
                    Secret,
                    field.Value?.ToString() ?? string.Empty,
                    StringComparison.Ordinal));
        });
}
