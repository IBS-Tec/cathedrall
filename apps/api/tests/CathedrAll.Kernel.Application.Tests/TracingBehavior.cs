namespace CathedrAll.Kernel.Application.Tests;

internal sealed class BehaviorRastreado(string nome, List<string> rastro)
    : IPipelineBehavior<RequisicaoFalsa, string>
{
    public async Task<string> HandleAsync(
        RequisicaoFalsa request,
        RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken)
    {
        rastro.Add($"{nome} antes");
        string resposta = await next();
        rastro.Add($"{nome} depois");

        return resposta;
    }
}
