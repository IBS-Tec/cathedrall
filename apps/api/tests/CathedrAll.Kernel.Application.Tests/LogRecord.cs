using Microsoft.Extensions.Logging;

namespace CathedrAll.Kernel.Application.Tests;

internal sealed record RegistroDeLog(
    LogLevel Nivel,
    string Mensagem,
    IReadOnlyList<KeyValuePair<string, object?>> Estado,
    Exception? Excecao);
