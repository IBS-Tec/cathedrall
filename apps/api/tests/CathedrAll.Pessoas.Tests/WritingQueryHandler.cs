using CathedrAll.Kernel.Application;
using CathedrAll.Pessoas.Domain;
using CathedrAll.Pessoas.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CathedrAll.Pessoas.Tests;

internal sealed class WritingQueryHandler(PessoasDbContext context)
    : IRequestHandler<FakeReadQuery, int>
{
    public async Task<int> HandleAsync(FakeReadQuery request, CancellationToken cancellationToken)
    {
        context.Pessoas.Add(new Pessoa(new PessoaId(Guid.CreateVersion7()), request.Nome));

        return await context.Pessoas.CountAsync(cancellationToken);
    }
}
