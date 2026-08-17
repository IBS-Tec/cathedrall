namespace CathedrAll.Kernel.Application.Tests;

internal sealed class ShortCircuitingBehavior(List<string> trace)
    : IPipelineBehavior<FakeRequest, string>
{
    public const string Response = "short-circuit";

    public Task<string> HandleAsync(
        FakeRequest request,
        RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken)
    {
        trace.Add("short before");

        return Task.FromResult(Response);
    }
}
