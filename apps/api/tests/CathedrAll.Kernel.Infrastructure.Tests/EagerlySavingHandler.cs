using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;

namespace CathedrAll.Kernel.Infrastructure.Tests;

internal sealed class EagerlySavingHandler(FakeDbContext context)
    : IRequestHandler<FakeWriteCommand, Result<string>>
{
    public static readonly Error Rejection = Error.Conflict(
        "Test.RejectedAfterSave",
        "O comando salvou por conta própria e depois foi rejeitado.");

    public async Task<Result<string>> HandleAsync(
        FakeWriteCommand request,
        CancellationToken cancellationToken)
    {
        context.Rows.Add(new FakeRow { Id = Guid.CreateVersion7(), Value = request.Value });

        await context.SaveChangesAsync(cancellationToken);

        return Result.Failure<string>(Rejection);
    }
}
