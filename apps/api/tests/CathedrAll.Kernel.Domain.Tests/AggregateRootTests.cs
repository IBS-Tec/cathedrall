namespace CathedrAll.Kernel.Domain.Tests;

public sealed class AggregateRootTests
{
    [Fact]
    public void Agregado_recem_criado_nao_deve_ter_eventos()
    {
        var aggregate = new FakeAggregate(Guid.CreateVersion7());

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void PopDomainEvents_deve_devolver_os_eventos_na_ordem_de_registro()
    {
        var aggregate = new FakeAggregate(Guid.CreateVersion7());
        var first = new FakeEvent("first");
        var second = new FakeEvent("second");
        IDomainEvent[] expected = [first, second];

        aggregate.Raise(first);
        aggregate.Raise(second);

        Assert.Equal(expected, aggregate.PopDomainEvents());
    }

    [Fact]
    public void PopDomainEvents_deve_esvaziar_o_agregado()
    {
        var aggregate = new FakeAggregate(Guid.CreateVersion7());
        aggregate.Raise(new FakeEvent("any"));

        aggregate.PopDomainEvents();

        Assert.Empty(aggregate.DomainEvents);
        Assert.Empty(aggregate.PopDomainEvents());
    }

    private sealed class FakeAggregate(Guid id) : AggregateRoot<Guid>(id)
    {
        public void Raise(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
    }

    private sealed record FakeEvent(string Name) : DomainEvent;
}
