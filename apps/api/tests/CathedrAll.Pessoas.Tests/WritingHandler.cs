using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;
using CathedrAll.Pessoas.Domain;
using CathedrAll.Pessoas.Infrastructure;

namespace CathedrAll.Pessoas.Tests;

internal sealed class WritingHandler(PessoasDbContext context)
    : IRequestHandler<FakeWriteCommand, Result<PessoaId>>
{
    public Task<Result<PessoaId>> HandleAsync(
        FakeWriteCommand request,
        CancellationToken cancellationToken)
    {
        Pessoa pessoa = new(new PessoaId(Guid.CreateVersion7()), request.Nome);

        context.Pessoas.Add(pessoa);

        return Task.FromResult(Result.Success(pessoa.Id));
    }
}
