namespace CathedrAll.Kernel.Application.Tests;

internal sealed class CancelingHandler : IRequestHandler<FakeRequest, string>
{
    public Task<string> HandleAsync(FakeRequest request, CancellationToken cancellationToken) =>
        throw new OperationCanceledException();
}
