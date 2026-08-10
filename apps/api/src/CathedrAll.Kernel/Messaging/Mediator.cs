using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Kernel.Messaging;

/// <summary>
/// Resolves the handler for a request and wraps it in the registered behaviors.
///
/// Deliberately small and dumb: reflection is used only to close the open generic types,
/// and the resulting <see cref="MethodInfo"/> is cached. There is no assembly magic and
/// no code generation here — if this file starts growing, the right move is to call the
/// handler directly from the endpoint (see ADR-0012).
/// </summary>
public sealed class Mediator(IServiceProvider provider) : IMediator
{
    private static readonly ConcurrentDictionary<Type, HandlerPlan> Plans = new();

    public Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plan = Plans.GetOrAdd(
            request.GetType(),
            static type => HandlerPlan.For(type, typeof(TResponse)));

        var handler = provider.GetRequiredService(plan.HandlerType);

        Task<TResponse> Next() =>
            (Task<TResponse>)plan.HandleMethod.Invoke(handler, [request, cancellationToken])!;

        var pipeline = (Func<Task<TResponse>>)Next;

        // Reversed so the first-registered behavior ends up outermost.
        foreach (var behavior in provider.GetServices(plan.BehaviorType).Reverse())
        {
            var inner = pipeline;
            var instance = behavior!;
            pipeline = () =>
                (Task<TResponse>)plan.BehaviorMethod.Invoke(
                    instance, [request, inner, cancellationToken])!;
        }

        return pipeline();
    }

    private sealed record HandlerPlan(
        Type HandlerType,
        MethodInfo HandleMethod,
        Type BehaviorType,
        MethodInfo BehaviorMethod)
    {
        public static HandlerPlan For(Type requestType, Type responseType)
        {
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
            var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType);

            return new HandlerPlan(
                handlerType,
                handlerType.GetMethod(nameof(IRequestHandler<IRequest<object>, object>.Handle))!,
                behaviorType,
                behaviorType.GetMethod(nameof(IPipelineBehavior<IRequest<object>, object>.Handle))!);
        }
    }
}
