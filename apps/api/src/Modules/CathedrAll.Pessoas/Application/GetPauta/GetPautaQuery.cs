using CathedrAll.Kernel.Application;

namespace CathedrAll.Pessoas.Application;

internal sealed record GetPautaQuery(DateOnly Date) : IQuery<PautaResponse>;
