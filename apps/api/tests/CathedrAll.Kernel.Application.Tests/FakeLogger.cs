using Microsoft.Extensions.Logging;

namespace CathedrAll.Kernel.Application.Tests;

internal sealed class LoggerFalso(List<RegistroDeLog> registros) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        registros.Add(new RegistroDeLog(
            logLevel,
            formatter(state, exception),
            state as IReadOnlyList<KeyValuePair<string, object?>> ?? [],
            exception));
}
