using CathedrAll.Kernel.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CathedrAll.Kernel.Application.Tests;

public sealed class LoggingBehaviorTests
{
    private const string Segredo = "cpf-do-membro";

    [Fact]
    public async Task Sucesso_deve_registrar_uma_entrada_de_information_com_o_nome_da_requisicao()
    {
        List<RegistroDeLog> registros = [];
        List<string> rastro = [];

        using ServiceProvider provedor = Cenario.Construir(servicos =>
        {
            servicos.AddSingleton<ILoggerFactory>(new FabricaDeLogFalsa(registros));
            servicos.AddLoggingBehavior();
            servicos.AddSingleton<IRequestHandler<RequisicaoFalsa, string>>(new HandlerFalso(rastro));
        });

        using IServiceScope escopo = provedor.CreateScope();

        await Cenario.SenderDoEscopo(escopo)
            .SendAsync<RequisicaoFalsa, string>(new RequisicaoFalsa("qualquer"), CancellationToken.None);

        RegistroDeLog registro = Assert.Single(registros);

        Assert.Equal(LogLevel.Information, registro.Nivel);
        Assert.Contains(nameof(RequisicaoFalsa), registro.Mensagem, StringComparison.Ordinal);
        Assert.Contains("sucesso", registro.Mensagem, StringComparison.Ordinal);
        Assert.Null(registro.Excecao);
    }

    [Fact]
    public async Task Nao_deve_registrar_o_conteudo_da_requisicao()
    {
        List<RegistroDeLog> registros = [];
        List<string> rastro = [];

        using ServiceProvider provedor = Cenario.Construir(servicos =>
        {
            servicos.AddSingleton<ILoggerFactory>(new FabricaDeLogFalsa(registros));
            servicos.AddLoggingBehavior();
            servicos.AddSingleton<IRequestHandler<RequisicaoFalsa, string>>(new HandlerFalso(rastro));
        });

        using IServiceScope escopo = provedor.CreateScope();

        await Cenario.SenderDoEscopo(escopo)
            .SendAsync<RequisicaoFalsa, string>(new RequisicaoFalsa(Segredo), CancellationToken.None);

        GarantirQueNaoVazou(registros);
    }

    [Fact]
    public async Task Excecao_deve_subir_e_ainda_assim_registrar_o_desfecho()
    {
        List<RegistroDeLog> registros = [];

        using ServiceProvider provedor = Cenario.Construir(servicos =>
        {
            servicos.AddSingleton<ILoggerFactory>(new FabricaDeLogFalsa(registros));
            servicos.AddLoggingBehavior();
            servicos.AddSingleton<IRequestHandler<RequisicaoFalsa, string>>(new HandlerQueLanca());
        });

        using IServiceScope escopo = provedor.CreateScope();

        ISender sender = Cenario.SenderDoEscopo(escopo);

        await Assert.ThrowsAsync<TimeoutException>(() =>
            sender.SendAsync<RequisicaoFalsa, string>(new RequisicaoFalsa(Segredo), CancellationToken.None));

        RegistroDeLog registro = Assert.Single(registros);

        Assert.Equal(LogLevel.Error, registro.Nivel);
        Assert.Contains("exceção", registro.Mensagem, StringComparison.Ordinal);
        Assert.DoesNotContain(HandlerQueLanca.MensagemDeFalha, registro.Mensagem, StringComparison.Ordinal);
        Assert.Null(registro.Excecao);
        GarantirQueNaoVazou(registros);
    }

    [Fact]
    public async Task Falha_de_negocio_deve_registrar_warning_com_o_codigo_do_erro()
    {
        List<RegistroDeLog> registros = [];

        using ServiceProvider provedor = Cenario.Construir(servicos =>
        {
            servicos.AddSingleton<ILoggerFactory>(new FabricaDeLogFalsa(registros));
            servicos.AddLoggingBehavior();
            servicos.AddSingleton<IRequestHandler<ComandoQueFalha, Result<string>>>(new HandlerQueRecusa());
        });

        using IServiceScope escopo = provedor.CreateScope();

        Result<string> resultado = await Cenario.SenderDoEscopo(escopo)
            .SendAsync<ComandoQueFalha, Result<string>>(new ComandoQueFalha(Segredo), CancellationToken.None);

        RegistroDeLog registro = Assert.Single(registros);
        KeyValuePair<string, object?> codigo = Assert.Single(registro.Estado, campo => campo.Key == "Codigo");

        Assert.True(resultado.IsFailure);
        Assert.Equal(LogLevel.Warning, registro.Nivel);
        Assert.Equal(HandlerQueRecusa.Recusa.Code, codigo.Value);
        Assert.DoesNotContain(
            HandlerQueRecusa.Recusa.Description,
            registro.Mensagem,
            StringComparison.Ordinal);
        GarantirQueNaoVazou(registros);
    }

    [Fact]
    public async Task Result_bem_sucedido_deve_registrar_information()
    {
        List<RegistroDeLog> registros = [];

        using ServiceProvider provedor = Cenario.Construir(servicos =>
        {
            servicos.AddSingleton<ILoggerFactory>(new FabricaDeLogFalsa(registros));
            servicos.AddLoggingBehavior();
            servicos.AddSingleton<IRequestHandler<ComandoQueFalha, Result<string>>>(new HandlerQueAceita());
        });

        using IServiceScope escopo = provedor.CreateScope();

        await Cenario.SenderDoEscopo(escopo)
            .SendAsync<ComandoQueFalha, Result<string>>(new ComandoQueFalha("qualquer"), CancellationToken.None);

        RegistroDeLog registro = Assert.Single(registros);

        Assert.Equal(LogLevel.Information, registro.Nivel);
        Assert.DoesNotContain("Codigo", registro.Mensagem, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Deve_registrar_a_duracao_como_campo_estruturado()
    {
        List<RegistroDeLog> registros = [];
        List<string> rastro = [];

        using ServiceProvider provedor = Cenario.Construir(servicos =>
        {
            servicos.AddSingleton<ILoggerFactory>(new FabricaDeLogFalsa(registros));
            servicos.AddLoggingBehavior();
            servicos.AddSingleton<IRequestHandler<RequisicaoFalsa, string>>(new HandlerFalso(rastro));
        });

        using IServiceScope escopo = provedor.CreateScope();

        await Cenario.SenderDoEscopo(escopo)
            .SendAsync<RequisicaoFalsa, string>(new RequisicaoFalsa("qualquer"), CancellationToken.None);

        RegistroDeLog registro = Assert.Single(registros);
        KeyValuePair<string, object?> duracao =
            Assert.Single(registro.Estado, campo => campo.Key == "DuracaoMs");

        Assert.True(Assert.IsType<double>(duracao.Value) >= 0);
    }

    [Fact]
    public async Task Deve_usar_uma_categoria_fixa()
    {
        List<RegistroDeLog> registros = [];
        List<string> rastro = [];
        using FabricaDeLogFalsa fabrica = new(registros);

        using ServiceProvider provedor = Cenario.Construir(servicos =>
        {
            servicos.AddSingleton<ILoggerFactory>(fabrica);
            servicos.AddLoggingBehavior();
            servicos.AddSingleton<IRequestHandler<RequisicaoFalsa, string>>(new HandlerFalso(rastro));
        });

        using IServiceScope escopo = provedor.CreateScope();

        await Cenario.SenderDoEscopo(escopo)
            .SendAsync<RequisicaoFalsa, string>(new RequisicaoFalsa("qualquer"), CancellationToken.None);

        Assert.Equal("CathedrAll.Kernel.Application.Pipeline", Assert.Single(fabrica.Categorias));
    }

    private static void GarantirQueNaoVazou(List<RegistroDeLog> registros) =>
        Assert.All(registros, registro =>
        {
            Assert.DoesNotContain(Segredo, registro.Mensagem, StringComparison.Ordinal);
            Assert.All(registro.Estado, campo =>
                Assert.DoesNotContain(
                    Segredo,
                    campo.Value?.ToString() ?? string.Empty,
                    StringComparison.Ordinal));
        });
}
