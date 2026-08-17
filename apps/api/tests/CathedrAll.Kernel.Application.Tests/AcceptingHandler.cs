using CathedrAll.Kernel.Domain;

namespace CathedrAll.Kernel.Application.Tests;

internal sealed class AcceptingHandler : IRequestHandler<FakeResultCommand, Result<string>>
{
    public const string Response = "accepted";

    public Task<Result<string>> HandleAsync(
        FakeResultCommand request,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(Response));
}
