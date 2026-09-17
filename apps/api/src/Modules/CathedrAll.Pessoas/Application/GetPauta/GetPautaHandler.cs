using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;
using CathedrAll.Pessoas.Domain;
using CathedrAll.Pessoas.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CathedrAll.Pessoas.Application;

internal sealed class GetPautaHandler(
    PessoasDbContext context,
    ISender sender)
    : IRequestHandler<GetPautaQuery, Result<PautaResponse>>
{
    public async Task<Result<PautaResponse>> HandleAsync(GetPautaQuery request, CancellationToken cancellationToken)
    {
        List<VisitanteDaPauta> visitantes = await context.Pessoas
            .Where(pessoa => pessoa.FundidaEmId == null)
            .Where(pessoa => pessoa.Vinculos.Any(
                vinculo => vinculo.Situacao == Situacao.Visitante &&
                vinculo.DataInicio == request.Date))
            .OrderBy(pessoa => pessoa.NomeNormalizado)
            .Select(pessoa => new VisitanteDaPauta(
                pessoa.Id.Value,
                pessoa.Nome,
                context.Pessoas
                    .Where(convidou => convidou.Id == pessoa.ConvidadoPorId)
                    .Select(convidou => new PessoaRef(convidou.Id.Value, convidou.Nome))
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        (DateOnly Monday, DateOnly Sunday) interval = WeekContaining(request.Date);

        Result<ListAniversariantesResponse> result = await sender
            .SendAsync<ListAniversariantesQuery, Result<ListAniversariantesResponse>>(
                new ListAniversariantesQuery(interval.Monday, interval.Sunday),
                cancellationToken);

        if (result.IsFailure)
        {
            return result.Error;
        }

        return new PautaResponse(
            visitantes,
            result.Value.Aniversariantes);
    }

    private static (DateOnly Monday, DateOnly Sunday) WeekContaining(DateOnly date)
    {
        int daysSinceMonday = date.DayOfWeek == DayOfWeek.Sunday
            ? 6
            : (int)date.DayOfWeek - 1;

        DateOnly monday = date.AddDays(-daysSinceMonday);
        DateOnly sunday = monday.AddDays(6);

        return (monday, sunday);
    }
}
