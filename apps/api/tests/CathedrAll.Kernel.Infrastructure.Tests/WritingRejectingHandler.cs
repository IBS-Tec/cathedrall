using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;

namespace CathedrAll.Kernel.Infrastructure.Tests;

internal sealed class WritingRejectingHandler(FakeDbContext context)
    : IRequestHandler<FakeWriteCommand, Result<string>>
{
    public static readonly Error Rejection = Error.Conflict(
        "Test.Rejected",
        "O comando escreveu e depois foi rejeitado.");

    public Task<Result<string>> HandleAsync(
        FakeWriteCommand request,
        CancellationToken cancellationToken)
    {
        context.Rows.Add(new FakeRow { Id = Guid.CreateVersion7(), Value = request.Value });

        return Task.FromResult(Result.Failure<string>(Rejection));
    }
}
