namespace CathedrAll.Kernel.Application.Tests;

internal sealed record FakeCommand(string Value) : ICommand<string>;
