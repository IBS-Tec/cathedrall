using CathedrAll.Kernel.Application;

namespace CathedrAll.Pessoas.Application;

internal sealed record GetFichaPessoaQuery(Guid PessoaId) : IQuery<FichaPessoa>;
