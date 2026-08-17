using System.Diagnostics;
using CathedrAll.Kernel.Domain;
using Microsoft.Extensions.Logging;

namespace CathedrAll.Kernel.Application;

internal sealed class LoggingBehavior<TRequest, TResponse>(ILoggerFactory factory)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const string Category = "CathedrAll.Kernel.Application.Pipeline";

    private const string Template = "Request {Request} finished with {Outcome} in {DurationMs} ms";

    private const string ErrorTemplate = "Request {Request} finished with {Outcome} in {DurationMs} ms, error {ErrorCode}";

    private readonly ILogger _logger = factory.CreateLogger(Category);

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        long start = Stopwatch.GetTimestamp();
        LogLevel logLevel = LogLevel.Error;
        string outcome = "exception";
        string? errorCode = null;

        try
        {
            TResponse response = await next();

            if (response is Result result && result.IsFailure)
            {
                logLevel = LogLevel.Warning;
                outcome = "failure";
                errorCode = result.Error.Code;
            }
            else
            {
                logLevel = LogLevel.Information;
                outcome = "success";
            }

            return response;
        }
        catch (OperationCanceledException)
        {
            logLevel = LogLevel.Information;
            outcome = "canceled";

            throw;
        }
        finally
        {
            if (_logger.IsEnabled(logLevel))
            {
                double duration = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

                if (errorCode is null)
                {
                    _logger.Log(logLevel, Template, typeof(TRequest).Name, outcome, duration);
                }
                else
                {
                    _logger.Log(logLevel, ErrorTemplate, typeof(TRequest).Name, outcome, duration, errorCode);
                }
            }
        }
    }
}
