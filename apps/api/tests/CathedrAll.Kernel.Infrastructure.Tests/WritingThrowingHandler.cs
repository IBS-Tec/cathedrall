using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;

namespace CathedrAll.Kernel.Infrastructure.Tests;

internal sealed class WritingThrowingHandler(FakeDbContext context)
    : IRequestHandler<FakeWriteCommand, Result<string>>
{
    public const string FailureMessage = "Falhou depois de escrever";

    public Task<Result<string>> HandleAsync(
        FakeWriteCommand request,
        CancellationToken cancellationToken)
    {
        context.Rows.Add(new FakeRow { Id = Guid.CreateVersion7(), Value = request.Value });

        throw new TimeoutException(FailureMessage);
    }
}
