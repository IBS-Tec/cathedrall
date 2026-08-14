namespace CathedrAll.Kernel.Domain;

public interface IDomainEvent
{
    Guid Id { get; }

    DateTimeOffset OccurredOn { get; }
}
