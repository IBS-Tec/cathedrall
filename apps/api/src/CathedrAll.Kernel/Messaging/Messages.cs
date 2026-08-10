namespace CathedrAll.Kernel.Messaging;

/// <summary>
/// A request that flows through the mediator pipeline. Implemented by commands and
/// queries inside each module.
/// </summary>
public interface IRequest<out TResponse>;

/// <summary>Handles exactly one request type. One handler per request, no exceptions.</summary>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Wraps every request. Behaviors run in registration order on the way in and in reverse
/// on the way out — the outermost registered behavior is the first to see the request and
/// the last to see the response.
/// </summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(
        TRequest request,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken);
}

public interface IMediator
{
    Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}
