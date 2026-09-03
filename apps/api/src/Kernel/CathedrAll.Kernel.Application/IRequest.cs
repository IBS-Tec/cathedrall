using CathedrAll.Kernel.Domain;

namespace CathedrAll.Kernel.Application;

#pragma warning disable S2326
public interface IRequest<TResponse>
#pragma warning restore S2326
{
}

public interface ICommandBase
{
}

public interface ICommand : ICommandBase, IRequest<Result>
{
}

public interface ICommand<TValue> : ICommandBase, IRequest<Result<TValue>>
{
}

public interface IQuery<TValue> : IRequest<Result<TValue>>
{
}
