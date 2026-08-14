namespace CathedrAll.Kernel.Domain.Tests;

public sealed class DomainEventTests
{
    [Fact]
    public void O_id_do_evento_deve_ser_o_mesmo_em_leituras_sucessivas()
    {
        var evento = new EventoFalso("qualquer");

        Guid primeiraLeitura = evento.Id;
        Guid segundaLeitura = evento.Id;

        Assert.Equal(primeiraLeitura, segundaLeitura);
    }

    [Fact]
    public void Eventos_distintos_devem_ter_ids_distintos()
    {
        var primeiro = new EventoFalso("qualquer");
        var segundo = new EventoFalso("qualquer");

        Assert.NotEqual(primeiro.Id, segundo.Id);
    }

    private sealed record EventoFalso(string Nome) : DomainEvent;
}
