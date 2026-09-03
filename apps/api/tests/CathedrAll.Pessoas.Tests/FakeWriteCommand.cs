using CathedrAll.Kernel.Application;
using CathedrAll.Pessoas.Domain;

namespace CathedrAll.Pessoas.Tests;

internal sealed record FakeWriteCommand(string Nome) : ICommand<PessoaId>;
