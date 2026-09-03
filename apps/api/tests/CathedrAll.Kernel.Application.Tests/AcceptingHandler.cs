using CathedrAll.Kernel.Domain;

namespace CathedrAll.Kernel.Application.Tests;

internal sealed class AcceptingHandler : IRequestHandler<FakeCommand, Result<string>>
{
    public const string Response = "accepted";

    public Task<Result<string>> HandleAsync(
        FakeCommand request,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(Response));
}
