namespace CathedrAll.Kernel.Application.Tests;

internal sealed class FakeCommandHandler(List<string> trace) : IRequestHandler<FakeCommand, string>
{
    public Task<string> HandleAsync(FakeCommand request, CancellationToken cancellationToken)
    {
        trace.Add("handler");

        return Task.FromResult(FakeHandler.Response);
    }
}
