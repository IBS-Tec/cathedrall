using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Kernel.Messaging;

public static class MediatorRegistration
{
    /// <summary>
    /// Registers the mediator and every <see cref="IRequestHandler{TRequest,TResponse}"/>
    /// found in the given assemblies.
    ///
    /// This is the only place that scans assemblies, and it scans for exactly one thing.
    /// Each module passes its own assembly explicitly — nothing is discovered behind
    /// your back.
    /// </summary>
    public static IServiceCollection AddMediator(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddSingleton<IMediator, Mediator>();

        foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
        {
            if (type is { IsAbstract: false, IsInterface: false })
            {
                foreach (var handled in type.GetInterfaces().Where(IsHandler))
                {
                    services.AddScoped(handled, type);
                }
            }
        }

        return services;

        static bool IsHandler(Type i) =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>);
    }

    /// <summary>
    /// Adds a behavior to the pipeline. Order matters: the first registered runs outermost.
    /// </summary>
    public static IServiceCollection AddPipelineBehavior(
        this IServiceCollection services,
        Type openGenericBehavior)
    {
        services.AddScoped(typeof(IPipelineBehavior<,>), openGenericBehavior);
        return services;
    }
}
