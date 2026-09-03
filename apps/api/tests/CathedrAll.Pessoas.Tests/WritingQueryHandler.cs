using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;
using CathedrAll.Pessoas.Domain;
using CathedrAll.Pessoas.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CathedrAll.Pessoas.Tests;

internal sealed class WritingQueryHandler(PessoasDbContext context)
    : IRequestHandler<FakeReadQuery, Result<int>>
{
    public async Task<Result<int>> HandleAsync(FakeReadQuery request, CancellationToken cancellationToken)
    {
        context.Pessoas.Add(new Pessoa(new PessoaId(Guid.CreateVersion7()), request.Nome));

        return await context.Pessoas.CountAsync(cancellationToken);
    }
}
