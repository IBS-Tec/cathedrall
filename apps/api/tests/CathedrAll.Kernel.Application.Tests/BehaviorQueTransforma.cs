namespace CathedrAll.Kernel.Application.Tests;

internal sealed class BehaviorQueTransforma : IPipelineBehavior<RequisicaoFalsa, string>
{
    public async Task<string> HandleAsync(
        RequisicaoFalsa request,
        RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken) => $"[{await next()}]";
}
