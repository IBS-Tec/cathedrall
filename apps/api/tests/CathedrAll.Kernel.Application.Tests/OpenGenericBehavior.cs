namespace CathedrAll.Kernel.Application.Tests;

internal sealed class BehaviorAberto<TRequest, TResponse>(List<string> rastro)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        rastro.Add("aberto antes");
        TResponse resposta = await next();
        rastro.Add("aberto depois");

        return resposta;
    }
}
