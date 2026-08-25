namespace CathedrAll.Kernel.Application;

public interface ICurrentUser
{
    Guid Id { get; }

    Papel Papel { get; }
}
