namespace CathedrAll.Kernel.Application.Tests;

internal sealed class BehaviorSoDeComando<TRequest, TResponse>(List<string> rastro)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        rastro.Add("comando antes");
        TResponse resposta = await next();
        rastro.Add("comando depois");

        return resposta;
    }
}
