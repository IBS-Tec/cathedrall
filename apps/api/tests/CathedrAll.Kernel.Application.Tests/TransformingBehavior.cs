namespace CathedrAll.Kernel.Application.Tests;

internal sealed class TransformingBehavior : IPipelineBehavior<FakeRequest, string>
{
    public async Task<string> HandleAsync(
        FakeRequest request,
        RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken) => $"[{await next()}]";
}
