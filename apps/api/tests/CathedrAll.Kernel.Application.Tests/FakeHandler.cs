namespace CathedrAll.Kernel.Application.Tests;

internal sealed class HandlerFalso(List<string> rastro) : IRequestHandler<RequisicaoFalsa, string>
{
    public const string Resposta = "resposta do handler";

    public RequisicaoFalsa? RequisicaoRecebida { get; private set; }

    public CancellationToken TokenRecebido { get; private set; }

    public Task<string> HandleAsync(RequisicaoFalsa request, CancellationToken cancellationToken)
    {
        RequisicaoRecebida = request;
        TokenRecebido = cancellationToken;
        rastro.Add("handler");

        return Task.FromResult(Resposta);
    }
}
