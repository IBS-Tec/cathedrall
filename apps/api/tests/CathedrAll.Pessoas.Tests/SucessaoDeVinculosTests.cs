using CathedrAll.Kernel.Domain;
using CathedrAll.Pessoas.Domain;

namespace CathedrAll.Pessoas.Tests;

public sealed class SucessaoDeVinculosTests
{
    private static readonly DateOnly Hoje = new(2026, 8, 25);

    [Fact]
    public void Primeiro_vinculo_deve_ser_aceito_sem_vigente()
    {
        Pessoa pessoa = NovaPessoa();

        Result resultado = pessoa.SucederVinculo(Situacao.Visitante, Hoje, null, Hoje);

        Assert.True(resultado.IsSuccess);

        VinculoIgreja vinculo = Assert.Single(pessoa.Vinculos);

        Assert.Equal(Situacao.Visitante, vinculo.Situacao);
        Assert.Equal(Hoje, vinculo.DataInicio);
        Assert.Null(vinculo.DataFim);
    }

    [Fact]
    public void Suceder_deve_encerrar_o_vinculo_vigente()
    {
        DateOnly chegada = Hoje.AddDays(-30);
        Pessoa pessoa = ComVinculo(Situacao.Visitante, chegada);

        pessoa.SucederVinculo(Situacao.Membro, Hoje, null, Hoje);

        Assert.Equal(Hoje, Vinculo(pessoa, Situacao.Visitante).DataFim);
        Assert.Null(Vinculo(pessoa, Situacao.Membro).DataFim);
    }

    [Fact]
    public void Sequencia_de_sucessoes_deve_deixar_um_unico_vigente()
    {
        Pessoa pessoa = ComVinculo(Situacao.Visitante, Hoje.AddDays(-90));

        pessoa.SucederVinculo(Situacao.Membro, Hoje.AddDays(-60), null, Hoje);
        pessoa.SucederVinculo(Situacao.Afastado, Hoje.AddDays(-30), "Mudou de cidade", Hoje);
        pessoa.SucederVinculo(Situacao.Membro, Hoje, null, Hoje);

        Assert.Equal(4, pessoa.Vinculos.Count);
        Assert.Single(pessoa.Vinculos, vinculo => vinculo.DataFim is null);
    }

    [Fact]
    public void DataInicio_do_novo_deve_ser_a_DataFim_do_anterior()
    {
        Pessoa pessoa = ComVinculo(Situacao.Visitante, Hoje.AddDays(-30));

        pessoa.SucederVinculo(Situacao.Membro, Hoje, null, Hoje);

        Assert.Equal(
            Vinculo(pessoa, Situacao.Visitante).DataFim,
            Vinculo(pessoa, Situacao.Membro).DataInicio);
    }

    [Fact]
    public void Data_anterior_ao_inicio_do_vigente_deve_ser_recusada()
    {
        DateOnly chegada = Hoje.AddDays(-30);
        Pessoa pessoa = ComVinculo(Situacao.Visitante, chegada);

        Result resultado = pessoa.SucederVinculo(Situacao.Membro, chegada.AddDays(-1), null, Hoje);

        Assert.True(resultado.IsFailure);
        Assert.Equal("Pessoa.DataRetroativa", resultado.Error.Code);
    }

    [Fact]
    public void Data_igual_ao_inicio_do_vigente_deve_ser_aceita()
    {
        DateOnly chegada = Hoje.AddDays(-10);
        Pessoa pessoa = ComVinculo(Situacao.Visitante, chegada);

        Result resultado = pessoa.SucederVinculo(Situacao.Membro, chegada, null, Hoje);

        Assert.True(resultado.IsSuccess);
        Assert.Equal(chegada, Vinculo(pessoa, Situacao.Visitante).DataFim);
    }

    [Fact]
    public void Data_futura_deve_ser_recusada()
    {
        Pessoa pessoa = NovaPessoa();

        Result resultado = pessoa.SucederVinculo(Situacao.Visitante, Hoje.AddDays(1), null, Hoje);

        Assert.True(resultado.IsFailure);
        Assert.Equal("Pessoa.DataFutura", resultado.Error.Code);
        Assert.Empty(pessoa.Vinculos);
    }

    [Fact]
    public void Data_de_hoje_deve_ser_aceita()
    {
        Pessoa pessoa = NovaPessoa();

        Result resultado = pessoa.SucederVinculo(Situacao.Visitante, Hoje, null, Hoje);

        Assert.True(resultado.IsSuccess);
    }

    [Fact]
    public void Vinculo_encerrado_deve_preservar_a_situacao()
    {
        Pessoa pessoa = ComVinculo(Situacao.Visitante, Hoje.AddDays(-30));

        pessoa.SucederVinculo(Situacao.Membro, Hoje, null, Hoje);

        VinculoIgreja encerrado = Assert.Single(pessoa.Vinculos, vinculo => vinculo.DataFim is not null);

        Assert.Equal(Situacao.Visitante, encerrado.Situacao);
    }

    [Fact]
    public void Recusa_nao_deve_alterar_a_colecao_de_vinculos()
    {
        DateOnly chegada = Hoje.AddDays(-30);
        Pessoa pessoa = ComVinculo(Situacao.Visitante, chegada);

        pessoa.SucederVinculo(Situacao.Membro, chegada.AddDays(-1), null, Hoje);

        VinculoIgreja vinculo = Assert.Single(pessoa.Vinculos);

        Assert.Equal(chegada, vinculo.DataInicio);
        Assert.Null(vinculo.DataFim);
    }

    private static Pessoa NovaPessoa() =>
        new(new PessoaId(Guid.CreateVersion7()), "João Guedes");

    private static Pessoa ComVinculo(Situacao situacao, DateOnly dataInicio)
    {
        Pessoa pessoa = NovaPessoa();
        pessoa.SucederVinculo(situacao, dataInicio, null, Hoje);

        return pessoa;
    }

    private static VinculoIgreja Vinculo(Pessoa pessoa, Situacao situacao) =>
        pessoa.Vinculos.Single(vinculo => vinculo.Situacao == situacao);
}
