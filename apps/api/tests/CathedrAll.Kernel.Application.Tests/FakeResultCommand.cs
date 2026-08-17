using CathedrAll.Kernel.Domain;

namespace CathedrAll.Kernel.Application.Tests;

internal sealed record FakeResultCommand(string Value) : ICommand<Result<string>>;
