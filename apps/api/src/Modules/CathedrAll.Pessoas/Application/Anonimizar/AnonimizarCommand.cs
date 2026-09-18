using CathedrAll.Kernel.Application;

namespace CathedrAll.Pessoas.Application;

internal sealed record AnonimizarCommand(Guid PessoaId) : ICommand;
