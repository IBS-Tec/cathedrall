using Microsoft.Extensions.Logging;

namespace CathedrAll.Kernel.Application.Tests;

internal sealed class FabricaDeLogFalsa(List<RegistroDeLog> registros) : ILoggerFactory
{
    private readonly List<string> _categorias = [];

    public IReadOnlyList<string> Categorias => _categorias;

    public ILogger CreateLogger(string categoryName)
    {
        _categorias.Add(categoryName);

        return new LoggerFalso(registros);
    }

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
