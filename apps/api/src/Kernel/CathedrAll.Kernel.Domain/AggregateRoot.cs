namespace CathedrAll.Kernel.Domain;

public abstract class AggregateRoot<TId>(TId id) : Entity<TId>(id), IAggregateRoot
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public IDomainEvent[] PopDomainEvents()
    {
        IDomainEvent[] events = [.. _domainEvents];
        _domainEvents.Clear();
        return events;
    }

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
