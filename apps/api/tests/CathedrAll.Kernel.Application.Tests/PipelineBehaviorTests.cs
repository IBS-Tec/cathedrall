using CathedrAll.Kernel.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Kernel.Application.Tests;

public sealed class PipelineBehaviorTests
{
    [Fact]
    public async Task Behaviors_devem_envolver_o_handler_na_ordem_de_registro()
    {
        List<string> trace = [];

        using ServiceProvider provider = Scenario.Build(services =>
        {
            services.AddSingleton<IRequestHandler<FakeRequest, string>>(new FakeHandler(trace));
            services.AddSingleton<IPipelineBehavior<FakeRequest, string>>(new TracingBehavior("A", trace));
            services.AddSingleton<IPipelineBehavior<FakeRequest, string>>(new TracingBehavior("B", trace));
            services.AddSingleton<IPipelineBehavior<FakeRequest, string>>(new TracingBehavior("C", trace));
        });

        using IServiceScope scope = provider.CreateScope();

        await Scenario.SenderFrom(scope)
            .SendAsync<FakeRequest, string>(new FakeRequest("qualquer"), CancellationToken.None);

        string[] expected =
        [
            "A before",
            "B before",
            "C before",
            "handler",
            "C after",
            "B after",
            "A after",
        ];

        Assert.Equal(expected, trace);
    }

    [Fact]
    public async Task Behavior_que_nao_chama_next_deve_impedir_o_handler()
    {
        List<string> trace = [];
        FakeHandler handler = new(trace);

        using ServiceProvider provider = Scenario.Build(services =>
        {
            services.AddSingleton<IRequestHandler<FakeRequest, string>>(handler);
            services.AddSingleton<IPipelineBehavior<FakeRequest, string>>(new ShortCircuitingBehavior(trace));
            services.AddSingleton<IPipelineBehavior<FakeRequest, string>>(new TracingBehavior("A", trace));
        });

        using IServiceScope scope = provider.CreateScope();

        string response = await Scenario.SenderFrom(scope)
            .SendAsync<FakeRequest, string>(new FakeRequest("qualquer"), CancellationToken.None);

        string[] expected = ["short before"];

        Assert.Equal(ShortCircuitingBehavior.Response, response);
        Assert.Equal(expected, trace);
        Assert.Null(handler.ReceivedRequest);
    }

    [Fact]
    public async Task Behavior_deve_conseguir_transformar_a_resposta_do_handler()
    {
        List<string> trace = [];

        using ServiceProvider provider = Scenario.Build(services =>
        {
            services.AddSingleton<IRequestHandler<FakeRequest, string>>(new FakeHandler(trace));
            services.AddSingleton<IPipelineBehavior<FakeRequest, string>, TransformingBehavior>();
        });

        using IServiceScope scope = provider.CreateScope();

        string response = await Scenario.SenderFrom(scope)
            .SendAsync<FakeRequest, string>(new FakeRequest("qualquer"), CancellationToken.None);

        Assert.Equal($"[{FakeHandler.Response}]", response);
    }

    [Fact]
    public async Task Behavior_registrado_como_generico_aberto_deve_entrar_na_cadeia()
    {
        List<string> trace = [];

        using ServiceProvider provider = Scenario.Build(services =>
        {
            services.AddSingleton(trace);
            services.AddSingleton<IRequestHandler<FakeRequest, string>>(new FakeHandler(trace));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(OpenGenericBehavior<,>));
        });

        using IServiceScope scope = provider.CreateScope();

        await Scenario.SenderFrom(scope)
            .SendAsync<FakeRequest, string>(new FakeRequest("qualquer"), CancellationToken.None);

        string[] expected = ["open before", "handler", "open after"];

        Assert.Equal(expected, trace);
    }

    [Fact]
    public async Task Behavior_restrito_a_comando_nao_deve_entrar_na_cadeia_de_uma_requisicao_comum()
    {
        List<string> trace = [];

        using ServiceProvider provider = Scenario.Build(services =>
        {
            services.AddSingleton(trace);
            services.AddSingleton<IRequestHandler<FakeRequest, string>>(new FakeHandler(trace));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CommandOnlyBehavior<,>));
        });

        using IServiceScope scope = provider.CreateScope();

        await Scenario.SenderFrom(scope)
            .SendAsync<FakeRequest, string>(new FakeRequest("qualquer"), CancellationToken.None);

        string[] expected = ["handler"];

        Assert.Equal(expected, trace);
    }

    [Fact]
    public async Task Behavior_restrito_a_comando_deve_entrar_na_cadeia_de_um_comando()
    {
        List<string> trace = [];

        using ServiceProvider provider = Scenario.Build(services =>
        {
            services.AddSingleton(trace);
            services.AddSingleton<IRequestHandler<FakeCommand, Result<string>>>(new FakeCommandHandler(trace));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CommandOnlyBehavior<,>));
        });

        using IServiceScope scope = provider.CreateScope();

        await Scenario.SenderFrom(scope)
            .SendAsync<FakeCommand, Result<string>>(new FakeCommand("qualquer"), CancellationToken.None);

        string[] expected = ["command before", "handler", "command after"];

        Assert.Equal(expected, trace);
    }
}
