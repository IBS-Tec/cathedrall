using CathedrAll.Kernel.Application;

namespace CathedrAll.Pessoas.Application;

internal sealed record SearchPessoasQuery(string Term) : IQuery<SearchPessoasResponse>;
