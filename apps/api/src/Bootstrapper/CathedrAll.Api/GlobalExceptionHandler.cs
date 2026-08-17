using CathedrAll.Kernel.Domain;
using Microsoft.AspNetCore.Diagnostics;

namespace CathedrAll.Api;

internal sealed class GlobalExceptionHandler(ILoggerFactory factory) : IExceptionHandler
{
    private const string Category = "CathedrAll.Api.UnhandledException";

    private static readonly Error UnexpectedFailure = Error.Failure(
        "Server.UnexpectedFailure",
        "Não foi possível concluir a operação.");

    private readonly ILogger _logger = factory.CreateLogger(Category);

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation(exception, "Request abandoned by the client.");

            return true;
        }

        if (httpContext.Response.HasStarted)
        {
            _logger.LogError(exception, "Unhandled exception after the response had started.");

            return false;
        }

        _logger.LogError(exception, "Unhandled exception.");

        await UnexpectedFailure.ToProblem().ExecuteAsync(httpContext);

        return true;
    }
}
