using CathedrAll.Kernel.Application;

namespace CathedrAll.Kernel.Infrastructure.Tests;

internal sealed class FakeTransactionBehavior<TRequest, TResponse>(FakeDbContext context)
    : TransactionBehavior<TRequest, TResponse>(context)
    where TRequest : IRequest<TResponse>, ICommandBase
{
}
