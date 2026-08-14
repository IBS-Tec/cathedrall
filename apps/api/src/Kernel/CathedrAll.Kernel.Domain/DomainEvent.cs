namespace CathedrAll.Kernel.Domain;

public abstract record DomainEvent : IDomainEvent
{
    public Guid Id { get; } = Guid.CreateVersion7();

    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
