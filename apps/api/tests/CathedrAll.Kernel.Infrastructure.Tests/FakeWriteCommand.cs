using CathedrAll.Kernel.Application;

namespace CathedrAll.Kernel.Infrastructure.Tests;

internal sealed record FakeWriteCommand(string Value) : ICommand<string>;
