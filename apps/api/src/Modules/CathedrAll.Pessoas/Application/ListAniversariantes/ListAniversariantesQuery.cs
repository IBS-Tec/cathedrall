using CathedrAll.Kernel.Application;

namespace CathedrAll.Pessoas.Application;

internal sealed record ListAniversariantesQuery(
    DateOnly From,
    DateOnly To)
    : IQuery<ListAniversariantesResponse>;
