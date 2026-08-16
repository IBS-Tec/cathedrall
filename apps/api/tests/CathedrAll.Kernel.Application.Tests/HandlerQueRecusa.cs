using CathedrAll.Kernel.Domain;

namespace CathedrAll.Kernel.Application.Tests;

internal sealed class HandlerQueRecusa : IRequestHandler<ComandoQueFalha, Result<string>>
{
    public static readonly Error Recusa = Error.Conflict(
        "Teste.Recusado",
        "Descrição que não deve aparecer no log.");

    public Task<Result<string>> HandleAsync(
        ComandoQueFalha request,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result.Failure<string>(Recusa));
}
