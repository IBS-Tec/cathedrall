using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;
using CathedrAll.Pessoas.Domain;
using CathedrAll.Pessoas.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CathedrAll.Pessoas.Application;

internal sealed class ListPessoasHandler(PessoasDbContext context)
    : IRequestHandler<ListPessoasQuery, Result<ListPessoasResponse>>
{
    private const int DefaultSize = 25;
    private const int MaximumSize = 50;

    public async Task<Result<ListPessoasResponse>> HandleAsync(
        ListPessoasQuery request,
        CancellationToken cancellationToken)
    {
        int page = Math.Max(request.Page ?? 1, 1);
        int size = Math.Clamp(request.Size ?? DefaultSize, 1, MaximumSize);

        IQueryable<Pessoa> pessoas = NomeFilter.Apply(
            context.Pessoas.Where(pessoa => pessoa.FundidaEmId == null),
            NomeFilter.Tokenize(request.Term));

        if (request.Situacao is Situacao situacao)
        {
            pessoas = pessoas.Where(pessoa => pessoa.Vinculos.Any(vinculo =>
                vinculo.DataFim == null && vinculo.Situacao == situacao));
        }

        if (!string.IsNullOrWhiteSpace(request.Bairro))
        {
            string bairro = TextNormalization.Normalize(request.Bairro);

            pessoas = pessoas.Where(pessoa => pessoa.Endereco!.BairroNormalizado == bairro);
        }

        int total = await pessoas.CountAsync(cancellationToken);

        List<PessoaDaLista> items = await pessoas
            .OrderBy(pessoa => pessoa.NomeNormalizado)
            .ThenBy(pessoa => pessoa.Id)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(pessoa => new PessoaDaLista(
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
                pessoa.Endereco!.Bairro))
            .ToListAsync(cancellationToken);

        return new ListPessoasResponse(items, page, size, total);
    }
}
