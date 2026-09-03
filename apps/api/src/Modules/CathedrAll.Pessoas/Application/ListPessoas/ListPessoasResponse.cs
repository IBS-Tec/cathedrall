namespace CathedrAll.Pessoas.Application;

internal sealed record ListPessoasResponse(
    IReadOnlyList<PessoaDaLista> Items,
    int Page,
    int Size,
    int Total);
