using CathedrAll.Kernel.Application;

namespace CathedrAll.Kernel.Infrastructure.Tests;

internal sealed class SecondFakeTransactionBehavior<TRequest, TResponse>(FakeDbContext context)
    : TransactionBehavior<TRequest, TResponse>(context)
    where TRequest : ICommand<TResponse>
{
}
