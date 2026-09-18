using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;
using CathedrAll.Pessoas.Domain;
using CathedrAll.Pessoas.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CathedrAll.Pessoas.Application;

internal sealed class AnonimizarCommandHandler(
    PessoasDbContext context) : IRequestHandler<AnonimizarCommand, Result>
{
    public async Task<Result> HandleAsync(AnonimizarCommand request, CancellationToken cancellationToken)
    {
        var pessoaId = new PessoaId(request.PessoaId);

        Pessoa? pessoa = await context.Pessoas
            .SingleOrDefaultAsync(pessoa => pessoa.Id == pessoaId, cancellationToken);

        if (pessoa is null)
        {
            return Result.Failure(PessoaErrors.NotFound);
        }

        Result anonimizarResult = pessoa.Anonimizar();

        if (anonimizarResult.IsFailure)
        {
            return anonimizarResult;
        }

        return Result.Success();
    }
}
