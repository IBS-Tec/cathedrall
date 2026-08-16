using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Kernel.Application.Tests;

public sealed class SenderTests
{
    [Fact]
    public async Task Deve_despachar_ao_handler_e_devolver_a_resposta()
    {
        List<string> rastro = [];
        HandlerFalso handler = new(rastro);

        using ServiceProvider provedor = Cenario.Construir(servicos =>
            servicos.AddSingleton<IRequestHandler<RequisicaoFalsa, string>>(handler));

        using IServiceScope escopo = provedor.CreateScope();

        string resposta = await Cenario.SenderDoEscopo(escopo)
            .SendAsync<RequisicaoFalsa, string>(new RequisicaoFalsa("qualquer"), CancellationToken.None);

        Assert.Equal(HandlerFalso.Resposta, resposta);
        Assert.Equal("qualquer", handler.RequisicaoRecebida?.Valor);
    }

    [Fact]
    public async Task Deve_repassar_o_token_de_cancelamento_ao_handler()
    {
        List<string> rastro = [];
        HandlerFalso handler = new(rastro);

        using ServiceProvider provedor = Cenario.Construir(servicos =>
        {
            servicos.AddSingleton<IRequestHandler<RequisicaoFalsa, string>>(handler);
            servicos.AddSingleton<IPipelineBehavior<RequisicaoFalsa, string>>(new BehaviorRastreado("A", rastro));
        });

        using CancellationTokenSource fonte = new();
        using IServiceScope escopo = provedor.CreateScope();

        await Cenario.SenderDoEscopo(escopo)
            .SendAsync<RequisicaoFalsa, string>(new RequisicaoFalsa("qualquer"), fonte.Token);

        Assert.Equal(fonte.Token, handler.TokenRecebido);
    }

    [Fact]
    public async Task Handler_nao_registrado_deve_lancar_excecao()
    {
        using ServiceProvider provedor = Cenario.Construir();
        using IServiceScope escopo = provedor.CreateScope();

        ISender sender = Cenario.SenderDoEscopo(escopo);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync<RequisicaoFalsa, string>(new RequisicaoFalsa("qualquer"), CancellationToken.None));
    }

    [Fact]
    public async Task Excecao_do_handler_deve_subir_intacta_pela_cadeia()
    {
        List<string> rastro = [];

        using ServiceProvider provedor = Cenario.Construir(servicos =>
        {
            servicos.AddSingleton<IRequestHandler<RequisicaoFalsa, string>>(new HandlerQueLanca());
            servicos.AddSingleton<IPipelineBehavior<RequisicaoFalsa, string>>(new BehaviorRastreado("A", rastro));
        });

        using IServiceScope escopo = provedor.CreateScope();

        ISender sender = Cenario.SenderDoEscopo(escopo);

        TimeoutException excecao = await Assert.ThrowsAsync<TimeoutException>(() =>
            sender.SendAsync<RequisicaoFalsa, string>(new RequisicaoFalsa("qualquer"), CancellationToken.None));

        Assert.Equal(HandlerQueLanca.MensagemDeFalha, excecao.Message);

        string[] esperado = ["A antes"];

        Assert.Equal(esperado, rastro);
    }

    [Fact]
    public async Task Deve_resolver_handler_registrado_como_scoped()
    {
        using ServiceProvider provedor = Cenario.Construir(servicos =>
        {
            servicos.AddScoped<List<string>>();
            servicos.AddScoped<IRequestHandler<RequisicaoFalsa, string>, HandlerFalso>();
        });

        using IServiceScope escopo = provedor.CreateScope();

        string resposta = await Cenario.SenderDoEscopo(escopo)
            .SendAsync<RequisicaoFalsa, string>(new RequisicaoFalsa("qualquer"), CancellationToken.None);

        Assert.Equal(HandlerFalso.Resposta, resposta);
    }
}
