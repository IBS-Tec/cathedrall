using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CathedrAll.Kernel.Infrastructure;

public abstract class TransactionBehavior<TRequest, TResponse>(DbContext context)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICommandBase
{
    private readonly DbContext _context = context;

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        TResponse response = await next();

        if (response is Result { IsFailure: true })
        {
            await transaction.RollbackAsync(cancellationToken);

            return response;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return response;
    }
}
