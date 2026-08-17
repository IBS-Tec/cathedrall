using Microsoft.Extensions.Logging;

namespace CathedrAll.Kernel.Application.Tests;

internal sealed class FakeLoggerFactory(List<LogRecord> records) : ILoggerFactory
{
    private readonly List<string> _categories = [];

    public IReadOnlyList<string> Categories => _categories;

    public ILogger CreateLogger(string categoryName)
    {
        _categories.Add(categoryName);

        return new FakeLogger(records);
    }

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
