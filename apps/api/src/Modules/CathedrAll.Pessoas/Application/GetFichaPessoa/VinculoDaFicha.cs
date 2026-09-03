using CathedrAll.Pessoas.Domain;

namespace CathedrAll.Pessoas.Application;

internal sealed record VinculoDaFicha(
    Situacao Situacao,
    DateOnly DataInicio,
    DateOnly? DataFim,
    string? Motivo);
