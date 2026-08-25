using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Infrastructure;

namespace CathedrAll.Pessoas.Infrastructure;

internal sealed class PessoasTransactionBehavior<TRequest, TResponse>(PessoasDbContext context)
    : TransactionBehavior<TRequest, TResponse>(context)
    where TRequest : ICommand<TResponse>
{
}
