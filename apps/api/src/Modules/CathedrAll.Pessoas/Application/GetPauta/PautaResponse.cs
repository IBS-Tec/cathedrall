namespace CathedrAll.Pessoas.Application;

internal sealed record PautaResponse(
    IReadOnlyList<VisitanteDaPauta> Visitantes,
    IReadOnlyList<Aniversariante> Aniversariantes);
