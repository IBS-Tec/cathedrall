using CathedrAll.Kernel.Domain;

namespace CathedrAll.Kernel.Application.Tests;

internal sealed class RejectingHandler : IRequestHandler<FakeCommand, Result<string>>
{
    public static readonly Error Rejection = Error.Conflict(
        "Test.Rejected",
        "Descrição que não deve aparecer no log.");

    public Task<Result<string>> HandleAsync(
        FakeCommand request,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result.Failure<string>(Rejection));
}
