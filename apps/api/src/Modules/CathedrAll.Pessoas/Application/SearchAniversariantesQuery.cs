using CathedrAll.Kernel.Application;

namespace CathedrAll.Pessoas.Application;

internal sealed record SearchAniversariantesQuery(
    DateOnly From,
    DateOnly To)
    : IQuery<SearchAniversariantesResponse>;
