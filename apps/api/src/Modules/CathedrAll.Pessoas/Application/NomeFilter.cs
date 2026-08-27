using CathedrAll.Pessoas.Domain;

namespace CathedrAll.Pessoas.Application;

internal static class NomeFilter
{
    internal static string[] Tokenize(string? term) =>
        TextNormalization.Normalize(term ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

    internal static IQueryable<Pessoa> Apply(IQueryable<Pessoa> pessoas, string[] tokens)
    {
        foreach (string token in tokens)
        {
            pessoas = pessoas.Where(pessoa =>
                pessoa.NomeNormalizado.StartsWith(token)
                || pessoa.NomeNormalizado.Contains(" " + token));
        }

        return pessoas;
    }
}
