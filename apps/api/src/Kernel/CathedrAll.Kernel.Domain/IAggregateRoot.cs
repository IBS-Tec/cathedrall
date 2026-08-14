namespace CathedrAll.Kernel.Domain;

public interface IAggregateRoot
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    IDomainEvent[] PopDomainEvents();
}
