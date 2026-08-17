using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;

namespace CathedrAll.Kernel.Infrastructure.Tests;

internal sealed class WritingHandler(FakeDbContext context)
    : IRequestHandler<FakeWriteCommand, Result<string>>
{
    public const string Response = "written";

    public Task<Result<string>> HandleAsync(
        FakeWriteCommand request,
        CancellationToken cancellationToken)
    {
        context.Rows.Add(new FakeRow { Id = Guid.CreateVersion7(), Value = request.Value });

        return Task.FromResult(Result.Success(Response));
    }
}
