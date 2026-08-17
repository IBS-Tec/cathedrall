using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;

namespace CathedrAll.Kernel.Infrastructure.Tests;

internal sealed record FakeWriteCommand(string Value) : ICommand<Result<string>>;
