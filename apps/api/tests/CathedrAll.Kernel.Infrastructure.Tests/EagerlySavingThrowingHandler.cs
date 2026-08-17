using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;

namespace CathedrAll.Kernel.Infrastructure.Tests;

internal sealed class EagerlySavingThrowingHandler(FakeDbContext context)
    : IRequestHandler<FakeWriteCommand, Result<string>>
{
    public const string FailureMessage = "Falhou depois de salvar";

    public async Task<Result<string>> HandleAsync(
        FakeWriteCommand request,
        CancellationToken cancellationToken)
    {
        context.Rows.Add(new FakeRow { Id = Guid.CreateVersion7(), Value = request.Value });

        await context.SaveChangesAsync(cancellationToken);

        throw new TimeoutException(FailureMessage);
    }
}
