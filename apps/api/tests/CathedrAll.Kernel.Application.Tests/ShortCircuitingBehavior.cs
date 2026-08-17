namespace CathedrAll.Kernel.Application.Tests;

internal sealed class BehaviorQueCurtoCircuita(List<string> rastro)
    : IPipelineBehavior<RequisicaoFalsa, string>
{
    public const string Resposta = "curto-circuito";

    public Task<string> HandleAsync(
        RequisicaoFalsa request,
        RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken)
    {
        rastro.Add("curto antes");

        return Task.FromResult(Resposta);
    }
}
