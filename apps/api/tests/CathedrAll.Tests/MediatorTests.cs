using CathedrAll.Kernel.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CathedrAll.Tests;

public class MediatorTests
{
    private sealed record Somar(int A, int B) : IRequest<int>;

    private sealed class SomarHandler : IRequestHandler<Somar, int>
    {
        public Task<int> Handle(Somar request, CancellationToken cancellationToken) =>
            Task.FromResult(request.A + request.B);
    }

    /// Registra o que aconteceu, para provar a ordem do pipeline.
    private sealed class Trilha
    {
        public List<string> Passos { get; } = [];
    }

    private sealed class BehaviorExterno<TRequest, TResponse>(Trilha trilha)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> Handle(
            TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken)
        {
            trilha.Passos.Add("externo:entra");
            var r = await next();
            trilha.Passos.Add("externo:sai");
            return r;
        }
    }

    private sealed class BehaviorInterno<TRequest, TResponse>(Trilha trilha)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> Handle(
            TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken)
        {
            trilha.Passos.Add("interno:entra");
            var r = await next();
            trilha.Passos.Add("interno:sai");
            return r;
        }
    }

    private static ServiceProvider Montar(Action<IServiceCollection>? extra = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Trilha>();
        services.AddMediator(typeof(MediatorTests).Assembly);
        extra?.Invoke(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Resolve_o_handler_e_devolve_a_resposta()
    {
        var provider = Montar();
        var mediator = provider.GetRequiredService<IMediator>();

        var resultado = await mediator.Send(new Somar(2, 3));

        Assert.Equal(5, resultado);
    }

    [Fact]
    public async Task Primeiro_behavior_registrado_e_o_mais_externo()
    {
        var provider = Montar(s =>
        {
            s.AddPipelineBehavior(typeof(BehaviorExterno<,>));
            s.AddPipelineBehavior(typeof(BehaviorInterno<,>));
        });

        var mediator = provider.GetRequiredService<IMediator>();
        await mediator.Send(new Somar(1, 1));

        var trilha = provider.GetRequiredService<Trilha>();
        Assert.Equal(
            ["externo:entra", "interno:entra", "interno:sai", "externo:sai"],
            trilha.Passos);
    }

    [Fact]
    public async Task Requisicao_sem_handler_falha_de_forma_explicita()
    {
        var provider = Montar();
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.Send(new SemHandler()));
    }

    private sealed record SemHandler : IRequest<string>;
}
