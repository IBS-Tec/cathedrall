using CathedrAll.Kernel.Application;
using CathedrAll.Pessoas.Domain;
using CathedrAll.Pessoas.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CathedrAll.Pessoas.Application;

internal sealed class SearchAniversariantesHandler(PessoasDbContext context)
    : IRequestHandler<SearchAniversariantesQuery, SearchAniversariantesResponse>
{
    private const int MaximumDays = 31;

    public async Task<SearchAniversariantesResponse> HandleAsync(
        SearchAniversariantesQuery request,
        CancellationToken cancellationToken)
    {
        Dictionary<int, DateOnly> intervalo = DiasDoIntervalo(request.From, request.To);
        int[] dias = [.. intervalo.Keys];

        IQueryable<Pessoa> elegiveis = context.Pessoas
            .Where(pessoa => pessoa.FundidaEmId == null)
            .Where(pessoa => !pessoa.Vinculos.Any(vinculo =>
                vinculo.DataFim == null &&
                (vinculo.Situacao == Situacao.Falecido || vinculo.Situacao == Situacao.Transferido)));

        List<Aniversariante> nascimentos = await elegiveis
            .Where(pessoa => pessoa.DataNascimento != null)
            .Where(pessoa => dias.Contains(
                pessoa.DataNascimento!.Value.Month * 100 + pessoa.DataNascimento.Value.Day))
            .Select(pessoa => new Aniversariante(
                pessoa.Id.Value,
                pessoa.Nome,
                TipoAniversario.Nascimento,
                pessoa.DataNascimento!.Value))
            .ToListAsync(cancellationToken);

        List<Aniversariante> casamentos = await elegiveis
            .Where(pessoa => pessoa.DataCasamento != null)
            .Where(pessoa => dias.Contains(
                pessoa.DataCasamento!.Value.Month * 100 + pessoa.DataCasamento.Value.Day))
            .Select(pessoa => new Aniversariante(
                pessoa.Id.Value,
                pessoa.Nome,
                TipoAniversario.Casamento,
                pessoa.DataCasamento!.Value))
            .ToListAsync(cancellationToken);

        return new SearchAniversariantesResponse(
            [.. nascimentos
                .Concat(casamentos)
                .Select(aniversario => aniversario with { Data = intervalo[MonthDay(aniversario.Data)] })
                .OrderBy(aniversario => aniversario.Data)
                .ThenBy(aniversario => aniversario.Tipo)
                .ThenBy(aniversario => aniversario.Nome)]);
    }

    private static Dictionary<int, DateOnly> DiasDoIntervalo(DateOnly from, DateOnly to)
    {
        const int vinteOitoDeFevereiro = 228;
        const int vinteNoveDeFevereiro = 229;

        if (to < from)
        {
            to = from;
        }
        else if (to > from.AddDays(MaximumDays - 1))
        {
            to = from.AddDays(MaximumDays - 1);
        }

        Dictionary<int, DateOnly> intervalo = [];

        for (DateOnly dia = from; dia <= to; dia = dia.AddDays(1))
        {
            intervalo[MonthDay(dia)] = dia;
        }

        if (intervalo.TryGetValue(vinteOitoDeFevereiro, out DateOnly vinteOito) &&
            !intervalo.ContainsKey(vinteNoveDeFevereiro))
        {
            intervalo[vinteNoveDeFevereiro] = vinteOito;
        }

        return intervalo;
    }

    private static int MonthDay(DateOnly data) => data.Month * 100 + data.Day;
}
