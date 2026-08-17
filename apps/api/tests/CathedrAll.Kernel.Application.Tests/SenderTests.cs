using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Kernel.Application.Tests;

public sealed class SenderTests
{
    [Fact]
    public async Task Deve_despachar_ao_handler_e_devolver_a_resposta()
    {
        List<string> trace = [];
        FakeHandler handler = new(trace);

        using ServiceProvider provider = Scenario.Build(services =>
            services.AddSingleton<IRequestHandler<FakeRequest, string>>(handler));

        using IServiceScope scope = provider.CreateScope();

        string response = await Scenario.SenderFrom(scope)
            .SendAsync<FakeRequest, string>(new FakeRequest("qualquer"), CancellationToken.None);

        Assert.Equal(FakeHandler.Response, response);
        Assert.Equal("qualquer", handler.ReceivedRequest?.Value);
    }

    [Fact]
    public async Task Deve_repassar_o_token_de_cancelamento_ao_handler()
    {
        List<string> trace = [];
        FakeHandler handler = new(trace);

        using ServiceProvider provider = Scenario.Build(services =>
        {
            services.AddSingleton<IRequestHandler<FakeRequest, string>>(handler);
            services.AddSingleton<IPipelineBehavior<FakeRequest, string>>(new TracingBehavior("A", trace));
        });

        using CancellationTokenSource source = new();
        using IServiceScope scope = provider.CreateScope();

        await Scenario.SenderFrom(scope)
            .SendAsync<FakeRequest, string>(new FakeRequest("qualquer"), source.Token);

        Assert.Equal(source.Token, handler.ReceivedToken);
    }

    [Fact]
    public async Task Handler_nao_registrado_deve_lancar_excecao()
    {
        using ServiceProvider provider = Scenario.Build();
        using IServiceScope scope = provider.CreateScope();

        ISender sender = Scenario.SenderFrom(scope);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync<FakeRequest, string>(new FakeRequest("qualquer"), CancellationToken.None));
    }

    [Fact]
    public async Task Excecao_do_handler_deve_subir_intacta_pela_cadeia()
    {
        List<string> trace = [];

        using ServiceProvider provider = Scenario.Build(services =>
        {
            services.AddSingleton<IRequestHandler<FakeRequest, string>>(new ThrowingHandler());
            services.AddSingleton<IPipelineBehavior<FakeRequest, string>>(new TracingBehavior("A", trace));
        });

        using IServiceScope scope = provider.CreateScope();

        ISender sender = Scenario.SenderFrom(scope);

        TimeoutException exception = await Assert.ThrowsAsync<TimeoutException>(() =>
            sender.SendAsync<FakeRequest, string>(new FakeRequest("qualquer"), CancellationToken.None));

        Assert.Equal(ThrowingHandler.FailureMessage, exception.Message);

        string[] expected = ["A before"];

        Assert.Equal(expected, trace);
    }

    [Fact]
    public async Task Deve_resolver_handler_registrado_como_scoped()
    {
        using ServiceProvider provider = Scenario.Build(services =>
        {
            services.AddScoped<List<string>>();
            services.AddScoped<IRequestHandler<FakeRequest, string>, FakeHandler>();
        });

        using IServiceScope scope = provider.CreateScope();

        string response = await Scenario.SenderFrom(scope)
            .SendAsync<FakeRequest, string>(new FakeRequest("qualquer"), CancellationToken.None);

        Assert.Equal(FakeHandler.Response, response);
    }
}
