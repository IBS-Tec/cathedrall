using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;
using CathedrAll.Pessoas.Domain;
using CathedrAll.Pessoas.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CathedrAll.Pessoas.Application;

internal sealed class SearchPessoasHandler(PessoasDbContext context)
    : IRequestHandler<SearchPessoasQuery, Result<SearchPessoasResponse>>
{
    private const int MaximumResults = 10;

    public async Task<Result<SearchPessoasResponse>> HandleAsync(
        SearchPessoasQuery request,
        CancellationToken cancellationToken)
    {
        string[] tokens = NomeFilter.Tokenize(request.Term);

        if (tokens.Length == 0)
        {
            return new SearchPessoasResponse([]);
        }

        IQueryable<PessoaId> sobreviventes = NomeFilter
            .Apply(context.Pessoas, tokens)
            .Select(pessoa => pessoa.FundidaEmId ?? pessoa.Id);

        List<PessoaEncontrada> results = await context.Pessoas
            .Where(pessoa => sobreviventes.Contains(pessoa.Id))
            .OrderBy(pessoa => pessoa.NomeNormalizado)
            .ThenBy(pessoa => pessoa.Id)
            .Take(MaximumResults)
            .Select(pessoa => new PessoaEncontrada(
                pessoa.Id.Value,
                pessoa.Nome,
                pessoa.Vinculos
                    .Where(vinculo => vinculo.DataFim == null)
                    .Select(vinculo => vinculo.Situacao)
                    .FirstOrDefault(),
                pessoa.Vinculos
                    .Where(vinculo => vinculo.DataFim == null)
                    .Select(vinculo => vinculo.DataInicio)
                    .FirstOrDefault(),
                context.Pessoas
                    .Where(convidou => convidou.Id == pessoa.ConvidadoPorId)
                    .Select(convidou => new PessoaRef(convidou.Id.Value, convidou.Nome))
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new SearchPessoasResponse(results);
    }
}
