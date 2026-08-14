namespace CathedrAll.Kernel.Domain.Tests;

public sealed class AggregateRootTests
{
    [Fact]
    public void Agregado_recem_criado_nao_deve_ter_eventos()
    {
        var agregado = new AgregadoFalso(Guid.CreateVersion7());

        Assert.Empty(agregado.DomainEvents);
    }

    [Fact]
    public void PopDomainEvents_deve_devolver_os_eventos_na_ordem_de_registro()
    {
        var agregado = new AgregadoFalso(Guid.CreateVersion7());
        var primeiro = new EventoFalso("primeiro");
        var segundo = new EventoFalso("segundo");
        IDomainEvent[] esperados = [primeiro, segundo];

        agregado.Registrar(primeiro);
        agregado.Registrar(segundo);

        Assert.Equal(esperados, agregado.PopDomainEvents());
    }

    [Fact]
    public void PopDomainEvents_deve_esvaziar_o_agregado()
    {
        var agregado = new AgregadoFalso(Guid.CreateVersion7());
        agregado.Registrar(new EventoFalso("qualquer"));

        agregado.PopDomainEvents();

        Assert.Empty(agregado.DomainEvents);
        Assert.Empty(agregado.PopDomainEvents());
    }

    private sealed class AgregadoFalso(Guid id) : AggregateRoot<Guid>(id)
    {
        public void Registrar(IDomainEvent evento) => AddDomainEvent(evento);
    }

    private sealed record EventoFalso(string Nome) : DomainEvent;
}
