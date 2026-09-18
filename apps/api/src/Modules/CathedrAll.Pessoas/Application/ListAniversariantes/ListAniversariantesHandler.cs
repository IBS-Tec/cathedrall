using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;
using CathedrAll.Pessoas.Domain;
using CathedrAll.Pessoas.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CathedrAll.Pessoas.Application;

internal sealed class ListAniversariantesHandler(PessoasDbContext context)
    : IRequestHandler<ListAniversariantesQuery, Result<ListAniversariantesResponse>>
{
    private const int MaximumDays = 31;

    public async Task<Result<ListAniversariantesResponse>> HandleAsync(
        ListAniversariantesQuery request,
        CancellationToken cancellationToken)
    {
        Dictionary<int, DateOnly> interval = DaysOfInterval(request.From, request.To);
        int[] days = [.. interval.Keys];

        IQueryable<Pessoa> eligible = context.Pessoas
            .Where(pessoa => pessoa.FundidaEmId == null)
            .Where(pessoa => !pessoa.Vinculos.Any(vinculo =>
                vinculo.DataFim == null &&
                (vinculo.Situacao == Situacao.Falecido || vinculo.Situacao == Situacao.Transferido)));

        List<Aniversariante> nascimentos = await eligible
            .Where(pessoa => pessoa.DataNascimento != null)
            .Where(pessoa => days.Contains(
                pessoa.DataNascimento!.Value.Month * 100 + pessoa.DataNascimento.Value.Day))
            .Select(pessoa => new Aniversariante(
                pessoa.Id.Value,
                pessoa.Nome,
                TipoAniversario.Nascimento,
                pessoa.DataNascimento!.Value))
            .ToListAsync(cancellationToken);

        List<Aniversariante> casamentos = await eligible
            .Where(pessoa => pessoa.DataCasamento != null)
            .Where(pessoa => days.Contains(
                pessoa.DataCasamento!.Value.Month * 100 + pessoa.DataCasamento.Value.Day))
            .Select(pessoa => new Aniversariante(
                pessoa.Id.Value,
                pessoa.Nome,
                TipoAniversario.Casamento,
                pessoa.DataCasamento!.Value))
            .ToListAsync(cancellationToken);

        return new ListAniversariantesResponse(
            [.. nascimentos
                .Concat(casamentos)
                .Select(aniversario => aniversario with { Data = interval[MonthDay(aniversario.Data)] })
                .OrderBy(aniversario => aniversario.Data)
                .ThenBy(aniversario => aniversario.Tipo)
                .ThenBy(aniversario => aniversario.Nome)]);
    }

    private static Dictionary<int, DateOnly> DaysOfInterval(DateOnly from, DateOnly to)
    {
        const int februaryTwentyEight = 228;
        const int februaryTwentyNine = 229;

        if (to < from)
        {
            to = from;
        }
        else if (to > from.AddDays(MaximumDays - 1))
        {
            to = from.AddDays(MaximumDays - 1);
        }

        Dictionary<int, DateOnly> interval = [];

        for (DateOnly day = from; day <= to; day = day.AddDays(1))
        {
            interval[MonthDay(day)] = day;
        }

        if (interval.TryGetValue(februaryTwentyEight, out DateOnly twentyEight) &&
            !interval.ContainsKey(februaryTwentyNine))
        {
            interval[februaryTwentyNine] = twentyEight;
        }

        return interval;
    }

    private static int MonthDay(DateOnly date) => date.Month * 100 + date.Day;
}
