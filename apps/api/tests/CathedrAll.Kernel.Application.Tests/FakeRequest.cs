namespace CathedrAll.Kernel.Application.Tests;

internal sealed record FakeRequest(string Value) : IRequest<string>;
