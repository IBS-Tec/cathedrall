using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Kernel.Application.Tests;

public sealed class PipelineBehaviorTests
{
    [Fact]
    public async Task Behaviors_devem_envolver_o_handler_na_ordem_de_registro()
    {
        List<string> rastro = [];

        using ServiceProvider provedor = Cenario.Construir(servicos =>
        {
            servicos.AddSingleton<IRequestHandler<RequisicaoFalsa, string>>(new HandlerFalso(rastro));
            servicos.AddSingleton<IPipelineBehavior<RequisicaoFalsa, string>>(new BehaviorRastreado("A", rastro));
            servicos.AddSingleton<IPipelineBehavior<RequisicaoFalsa, string>>(new BehaviorRastreado("B", rastro));
            servicos.AddSingleton<IPipelineBehavior<RequisicaoFalsa, string>>(new BehaviorRastreado("C", rastro));
        });

        using IServiceScope escopo = provedor.CreateScope();

        await Cenario.SenderDoEscopo(escopo)
            .SendAsync<RequisicaoFalsa, string>(new RequisicaoFalsa("qualquer"), CancellationToken.None);

        string[] esperado =
        [
            "A antes",
            "B antes",
            "C antes",
            "handler",
            "C depois",
            "B depois",
            "A depois",
        ];

        Assert.Equal(esperado, rastro);
    }

    [Fact]
    public async Task Behavior_que_nao_chama_next_deve_impedir_o_handler()
    {
        List<string> rastro = [];
        HandlerFalso handler = new(rastro);

        using ServiceProvider provedor = Cenario.Construir(servicos =>
        {
            servicos.AddSingleton<IRequestHandler<RequisicaoFalsa, string>>(handler);
            servicos.AddSingleton<IPipelineBehavior<RequisicaoFalsa, string>>(new BehaviorQueCurtoCircuita(rastro));
            servicos.AddSingleton<IPipelineBehavior<RequisicaoFalsa, string>>(new BehaviorRastreado("A", rastro));
        });

        using IServiceScope escopo = provedor.CreateScope();

        string resposta = await Cenario.SenderDoEscopo(escopo)
            .SendAsync<RequisicaoFalsa, string>(new RequisicaoFalsa("qualquer"), CancellationToken.None);

        string[] esperado = ["curto antes"];

        Assert.Equal(BehaviorQueCurtoCircuita.Resposta, resposta);
        Assert.Equal(esperado, rastro);
        Assert.Null(handler.RequisicaoRecebida);
    }

    [Fact]
    public async Task Behavior_deve_conseguir_transformar_a_resposta_do_handler()
    {
        List<string> rastro = [];

        using ServiceProvider provedor = Cenario.Construir(servicos =>
        {
            servicos.AddSingleton<IRequestHandler<RequisicaoFalsa, string>>(new HandlerFalso(rastro));
            servicos.AddSingleton<IPipelineBehavior<RequisicaoFalsa, string>, BehaviorQueTransforma>();
        });

        using IServiceScope escopo = provedor.CreateScope();

        string resposta = await Cenario.SenderDoEscopo(escopo)
            .SendAsync<RequisicaoFalsa, string>(new RequisicaoFalsa("qualquer"), CancellationToken.None);

        Assert.Equal($"[{HandlerFalso.Resposta}]", resposta);
    }

    [Fact]
    public async Task Behavior_registrado_como_generico_aberto_deve_entrar_na_cadeia()
    {
        List<string> rastro = [];

        using ServiceProvider provedor = Cenario.Construir(servicos =>
        {
            servicos.AddSingleton(rastro);
            servicos.AddSingleton<IRequestHandler<RequisicaoFalsa, string>>(new HandlerFalso(rastro));
            servicos.AddScoped(typeof(IPipelineBehavior<,>), typeof(BehaviorAberto<,>));
        });

        using IServiceScope escopo = provedor.CreateScope();

        await Cenario.SenderDoEscopo(escopo)
            .SendAsync<RequisicaoFalsa, string>(new RequisicaoFalsa("qualquer"), CancellationToken.None);

        string[] esperado = ["aberto antes", "handler", "aberto depois"];

        Assert.Equal(esperado, rastro);
    }

    [Fact]
    public async Task Behavior_restrito_a_comando_nao_deve_entrar_na_cadeia_de_uma_requisicao_comum()
    {
        List<string> rastro = [];

        using ServiceProvider provedor = Cenario.Construir(servicos =>
        {
            servicos.AddSingleton(rastro);
            servicos.AddSingleton<IRequestHandler<RequisicaoFalsa, string>>(new HandlerFalso(rastro));
            servicos.AddScoped(typeof(IPipelineBehavior<,>), typeof(BehaviorSoDeComando<,>));
        });

        using IServiceScope escopo = provedor.CreateScope();

        await Cenario.SenderDoEscopo(escopo)
            .SendAsync<RequisicaoFalsa, string>(new RequisicaoFalsa("qualquer"), CancellationToken.None);

        string[] esperado = ["handler"];

        Assert.Equal(esperado, rastro);
    }

    [Fact]
    public async Task Behavior_restrito_a_comando_deve_entrar_na_cadeia_de_um_comando()
    {
        List<string> rastro = [];

        using ServiceProvider provedor = Cenario.Construir(servicos =>
        {
            servicos.AddSingleton(rastro);
            servicos.AddSingleton<IRequestHandler<ComandoFalso, string>>(new HandlerDeComandoFalso(rastro));
            servicos.AddScoped(typeof(IPipelineBehavior<,>), typeof(BehaviorSoDeComando<,>));
        });

        using IServiceScope escopo = provedor.CreateScope();

        await Cenario.SenderDoEscopo(escopo)
            .SendAsync<ComandoFalso, string>(new ComandoFalso("qualquer"), CancellationToken.None);

        string[] esperado = ["comando antes", "handler", "comando depois"];

        Assert.Equal(esperado, rastro);
    }
}
