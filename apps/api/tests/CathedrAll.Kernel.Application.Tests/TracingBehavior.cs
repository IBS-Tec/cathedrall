namespace CathedrAll.Kernel.Application.Tests;

internal sealed class TracingBehavior(string name, List<string> trace)
    : IPipelineBehavior<FakeRequest, string>
{
    public async Task<string> HandleAsync(
        FakeRequest request,
        RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken)
    {
        trace.Add($"{name} before");
        string response = await next();
        trace.Add($"{name} after");

        return response;
    }
}
