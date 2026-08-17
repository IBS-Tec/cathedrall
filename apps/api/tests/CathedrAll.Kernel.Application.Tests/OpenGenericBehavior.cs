namespace CathedrAll.Kernel.Application.Tests;

internal sealed class OpenGenericBehavior<TRequest, TResponse>(List<string> trace)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        trace.Add("open before");
        TResponse response = await next();
        trace.Add("open after");

        return response;
    }
}
