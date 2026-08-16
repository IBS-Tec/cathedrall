using System.Diagnostics;
using CathedrAll.Kernel.Domain;
using Microsoft.Extensions.Logging;

namespace CathedrAll.Kernel.Application;

internal sealed class LoggingBehavior<TRequest, TResponse>(ILoggerFactory factory)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const string Categoria = "CathedrAll.Kernel.Application.Pipeline";

    private const string Template = "Requisição {Requisicao} terminou com {Desfecho} em {DuracaoMs} ms";

    private const string TemplateErro = "Requisição {Requisicao} terminou com {Desfecho} em {DuracaoMs} ms, erro {Codigo}";

    private readonly ILogger _logger = factory.CreateLogger(Categoria);

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        long inicio = Stopwatch.GetTimestamp();
        LogLevel logLevel = LogLevel.Error;
        string desfecho = "exceção";
        string? codigo = null;

        try
        {
            TResponse response = await next();

            if (response is Result result && result.IsFailure)
            {
                logLevel = LogLevel.Warning;
                desfecho = "falha";
                codigo = result.Error.Code;
            }
            else
            {
                logLevel = LogLevel.Information;
                desfecho = "sucesso";
            }

            return response;
        }
        finally
        {
            if (_logger.IsEnabled(logLevel))
            {
                double duracao = Stopwatch.GetElapsedTime(inicio).TotalMilliseconds;

                if (codigo is null)
                {
                    _logger.Log(logLevel, Template, typeof(TRequest).Name, desfecho, duracao);
                }
                else
                {
                    _logger.Log(logLevel, TemplateErro, typeof(TRequest).Name, desfecho, duracao, codigo);
                }
            }
        }
    }
}
