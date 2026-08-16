namespace CathedrAll.Kernel.Application;

#pragma warning disable S2326
public interface IRequest<TResponse>
#pragma warning restore S2326
{
}

public interface ICommand<TResponse> : IRequest<TResponse>
{
}

public interface IQuery<TResponse> : IRequest<TResponse>
{
}
