namespace CathedrAll.Kernel.Application;

public interface ISender
{
    Task<TResponse> SendAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>;
}
