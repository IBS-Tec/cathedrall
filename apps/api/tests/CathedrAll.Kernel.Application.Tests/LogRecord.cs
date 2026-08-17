using Microsoft.Extensions.Logging;

namespace CathedrAll.Kernel.Application.Tests;

internal sealed record LogRecord(
    LogLevel Level,
    string Message,
    IReadOnlyList<KeyValuePair<string, object?>> State,
    Exception? Exception);
