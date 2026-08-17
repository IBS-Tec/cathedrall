using CathedrAll.Kernel.Domain;

namespace CathedrAll.Kernel.Application.Tests;

internal sealed class HandlerQueAceita : IRequestHandler<ComandoQueFalha, Result<string>>
{
    public const string Resposta = "aceito";

    public Task<Result<string>> HandleAsync(
        ComandoQueFalha request,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(Resposta));
}
