namespace CathedrAll.Kernel.Domain.Tests;

public sealed class DomainEventTests
{
    [Fact]
    public void O_id_do_evento_deve_ser_o_mesmo_em_leituras_sucessivas()
    {
        var domainEvent = new FakeEvent("any");

        Guid firstRead = domainEvent.Id;
        Guid secondRead = domainEvent.Id;

        Assert.Equal(firstRead, secondRead);
    }

    [Fact]
    public void Eventos_distintos_devem_ter_ids_distintos()
    {
        var first = new FakeEvent("any");
        var second = new FakeEvent("any");

        Assert.NotEqual(first.Id, second.Id);
    }

    private sealed record FakeEvent(string Name) : DomainEvent;
}
