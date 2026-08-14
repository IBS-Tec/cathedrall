namespace CathedrAll.Kernel.Domain.Tests;

public sealed class EntityTests
{
    [Fact]
    public void Entidades_do_mesmo_tipo_com_o_mesmo_id_devem_ser_iguais()
    {
        var id = Guid.CreateVersion7();
        var entidade = new EntidadeFalsa(id);
        var outraInstancia = new EntidadeFalsa(id);

        Assert.Equal(entidade, outraInstancia);
        Assert.True(entidade == outraInstancia);
        Assert.False(entidade != outraInstancia);
        Assert.Equal(entidade.GetHashCode(), outraInstancia.GetHashCode());
    }

    [Fact]
    public void Entidades_do_mesmo_tipo_com_ids_diferentes_devem_ser_diferentes()
    {
        var entidade = new EntidadeFalsa(Guid.CreateVersion7());
        var outra = new EntidadeFalsa(Guid.CreateVersion7());

        Assert.NotEqual(entidade, outra);
        Assert.True(entidade != outra);
    }

    [Fact]
    public void Entidades_de_tipos_diferentes_com_o_mesmo_id_devem_ser_diferentes()
    {
        var id = Guid.CreateVersion7();
        var entidade = new EntidadeFalsa(id);
        var deOutroTipo = new OutraEntidadeFalsa(id);

        Assert.False(entidade.Equals(deOutroTipo));
        Assert.False(deOutroTipo.Equals(entidade));
    }

    [Fact]
    public void HashSet_deve_tratar_instancias_com_o_mesmo_id_como_uma_so()
    {
        var id = Guid.CreateVersion7();

        HashSet<EntidadeFalsa> conjunto = [new EntidadeFalsa(id), new EntidadeFalsa(id)];

        Assert.Single(conjunto);
    }

    [Fact]
    public void Contains_deve_encontrar_outra_instancia_com_o_mesmo_id()
    {
        var id = Guid.CreateVersion7();
        List<EntidadeFalsa> lista = [new EntidadeFalsa(id)];

        Assert.Contains(new EntidadeFalsa(id), lista);
    }

    private sealed class EntidadeFalsa(Guid id) : Entity<Guid>(id);

    private sealed class OutraEntidadeFalsa(Guid id) : Entity<Guid>(id);
}
