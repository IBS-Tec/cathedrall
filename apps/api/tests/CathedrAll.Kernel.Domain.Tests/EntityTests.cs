namespace CathedrAll.Kernel.Domain.Tests;

public sealed class EntityTests
{
    [Fact]
    public void Entidades_do_mesmo_tipo_com_o_mesmo_id_devem_ser_iguais()
    {
        var id = Guid.CreateVersion7();
        var entity = new FakeEntity(id);
        var otherInstance = new FakeEntity(id);

        Assert.Equal(entity, otherInstance);
        Assert.True(entity == otherInstance);
        Assert.False(entity != otherInstance);
        Assert.Equal(entity.GetHashCode(), otherInstance.GetHashCode());
    }

    [Fact]
    public void Entidades_do_mesmo_tipo_com_ids_diferentes_devem_ser_diferentes()
    {
        var entity = new FakeEntity(Guid.CreateVersion7());
        var other = new FakeEntity(Guid.CreateVersion7());

        Assert.NotEqual(entity, other);
        Assert.True(entity != other);
    }

    [Fact]
    public void Entidades_de_tipos_diferentes_com_o_mesmo_id_devem_ser_diferentes()
    {
        var id = Guid.CreateVersion7();
        var entity = new FakeEntity(id);
        var ofAnotherType = new AnotherFakeEntity(id);

        Assert.False(entity.Equals(ofAnotherType));
        Assert.False(ofAnotherType.Equals(entity));
    }

    [Fact]
    public void HashSet_deve_tratar_instancias_com_o_mesmo_id_como_uma_so()
    {
        var id = Guid.CreateVersion7();

        HashSet<FakeEntity> set = [new FakeEntity(id), new FakeEntity(id)];

        Assert.Single(set);
    }

    [Fact]
    public void Contains_deve_encontrar_outra_instancia_com_o_mesmo_id()
    {
        var id = Guid.CreateVersion7();
        List<FakeEntity> list = [new FakeEntity(id)];

        Assert.Contains(new FakeEntity(id), list);
    }

    private sealed class FakeEntity(Guid id) : Entity<Guid>(id);

    private sealed class AnotherFakeEntity(Guid id) : Entity<Guid>(id);
}
