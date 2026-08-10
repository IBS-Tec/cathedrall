using System.Diagnostics;
using CathedrAll.Kernel.Messaging;
using Microsoft.Extensions.Logging;

namespace CathedrAll.Kernel.Behaviors;

/// <summary>
/// Logs every request with its duration.
///
/// Logs the request TYPE only — never its content. Requests carry names, phone numbers
/// and addresses of church members, and a log file is a copy of that data with none of
/// the access control the database has (see docs/arquitetura.md, LGPD).
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            logger.LogInformation("{Request} concluído em {Elapsed}ms", name, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception e)
        {
            logger.LogError(e, "{Request} falhou em {Elapsed}ms", name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
