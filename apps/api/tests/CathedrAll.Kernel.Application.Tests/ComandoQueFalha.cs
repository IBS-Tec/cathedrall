using CathedrAll.Kernel.Domain;

namespace CathedrAll.Kernel.Application.Tests;

internal sealed record ComandoQueFalha(string Valor) : ICommand<Result<string>>;
