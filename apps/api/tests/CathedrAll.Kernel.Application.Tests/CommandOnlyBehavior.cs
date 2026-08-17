namespace CathedrAll.Kernel.Application.Tests;

internal sealed class CommandOnlyBehavior<TRequest, TResponse>(List<string> trace)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        trace.Add("command before");
        TResponse response = await next();
        trace.Add("command after");

        return response;
    }
}
