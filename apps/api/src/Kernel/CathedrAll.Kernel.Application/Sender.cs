using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Kernel.Application;

internal sealed class Sender(IServiceProvider serviceProvider) : ISender
{
    public Task<TResponse> SendAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        IRequestHandler<TRequest, TResponse> handler =
            serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

        RequestHandlerDelegate<TResponse> next =
            () => handler.HandleAsync(request, cancellationToken);

        foreach (IPipelineBehavior<TRequest, TResponse> behavior in
            serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse())
        {
            RequestHandlerDelegate<TResponse> inner = next;
            next = () => behavior.HandleAsync(request, inner, cancellationToken);
        }

        return next();
    }
}
