namespace CathedrAll.Pessoas.Application;

internal sealed record SearchPessoasResponse(IReadOnlyList<PessoaEncontrada> Results);
