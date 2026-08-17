namespace CathedrAll.Kernel.Application.Tests;

internal sealed class HandlerDeComandoFalso(List<string> rastro) : IRequestHandler<ComandoFalso, string>
{
    public Task<string> HandleAsync(ComandoFalso request, CancellationToken cancellationToken)
    {
        rastro.Add("handler");

        return Task.FromResult(HandlerFalso.Resposta);
    }
}
