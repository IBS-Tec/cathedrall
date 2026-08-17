namespace CathedrAll.Kernel.Application.Tests;

internal sealed class FakeHandler(List<string> trace) : IRequestHandler<FakeRequest, string>
{
    public const string Response = "handler response";

    public FakeRequest? ReceivedRequest { get; private set; }

    public CancellationToken ReceivedToken { get; private set; }

    public Task<string> HandleAsync(FakeRequest request, CancellationToken cancellationToken)
    {
        ReceivedRequest = request;
        ReceivedToken = cancellationToken;
        trace.Add("handler");

        return Task.FromResult(Response);
    }
}
