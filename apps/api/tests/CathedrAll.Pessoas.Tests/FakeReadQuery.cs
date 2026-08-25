using CathedrAll.Kernel.Application;

namespace CathedrAll.Pessoas.Tests;

internal sealed record FakeReadQuery(string Nome) : IQuery<int>;
