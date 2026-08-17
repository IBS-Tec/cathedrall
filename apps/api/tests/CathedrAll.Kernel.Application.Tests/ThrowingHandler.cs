namespace CathedrAll.Kernel.Application.Tests;

internal sealed class ThrowingHandler : IRequestHandler<FakeRequest, string>
{
    public const string FailureMessage = "Postgres is down";

    public Task<string> HandleAsync(FakeRequest request, CancellationToken cancellationToken) =>
        throw new TimeoutException(FailureMessage);
}
