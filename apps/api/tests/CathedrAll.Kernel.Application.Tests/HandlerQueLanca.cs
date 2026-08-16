namespace CathedrAll.Kernel.Application.Tests;

internal sealed class HandlerQueLanca : IRequestHandler<RequisicaoFalsa, string>
{
    public const string MensagemDeFalha = "Postgres fora do ar";

    public Task<string> HandleAsync(RequisicaoFalsa request, CancellationToken cancellationToken) =>
        throw new TimeoutException(MensagemDeFalha);
}
