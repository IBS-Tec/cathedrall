using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;
using CathedrAll.Pessoas.Domain;

namespace CathedrAll.Pessoas.Tests;

internal sealed record FakeWriteCommand(string Nome) : ICommand<Result<PessoaId>>;
