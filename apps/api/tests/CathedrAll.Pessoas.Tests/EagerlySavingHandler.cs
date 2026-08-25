using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;
using CathedrAll.Pessoas.Domain;
using CathedrAll.Pessoas.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CathedrAll.Pessoas.Tests;

internal sealed class EagerlySavingHandler(PessoasDbContext context)
    : IRequestHandler<FakeWriteCommand, Result<PessoaId>>
{
    public static readonly Error Rejection = Error.Conflict(
        "Test.Rejected",
        "O comando gravou, salvou e só então foi rejeitado.");

    public async Task<Result<PessoaId>> HandleAsync(
        FakeWriteCommand request,
        CancellationToken cancellationToken)
    {
        context.Pessoas.Add(new Pessoa(new PessoaId(Guid.CreateVersion7()), request.Nome));

        await context.SaveChangesAsync(cancellationToken);

        return Result.Failure<PessoaId>(Rejection);
    }
}
