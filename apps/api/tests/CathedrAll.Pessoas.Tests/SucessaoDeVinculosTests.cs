using CathedrAll.Kernel.Domain;
using CathedrAll.Pessoas.Domain;

namespace CathedrAll.Pessoas.Tests;

public sealed class SucessaoDeVinculosTests
{
    private static readonly DateOnly Hoje = new(2026, 8, 25);

    [Fact]
    public void Primeiro_vinculo_deve_ser_aceito_sem_vigente()
    {
        Pessoa pessoa = SemVinculo();

        Result resultado = pessoa.RegistrarApresentacao(Hoje, Hoje);

        Assert.True(resultado.IsSuccess);

        VinculoIgreja vinculo = Assert.Single(pessoa.Vinculos);

        Assert.Equal(Situacao.Membro, vinculo.Situacao);
        Assert.Equal(Hoje, vinculo.DataInicio);
        Assert.Null(vinculo.DataFim);
    }

    [Fact]
    public void Suceder_deve_encerrar_o_vinculo_vigente()
    {
        Pessoa pessoa = Visitante(Hoje.AddDays(-30));

        pessoa.RegistrarApresentacao(Hoje, Hoje);

        Assert.Equal(Hoje, Vinculo(pessoa, Situacao.Visitante).DataFim);
        Assert.Null(Vinculo(pessoa, Situacao.Membro).DataFim);
    }

    [Fact]
    public void Sequencia_de_sucessoes_deve_deixar_um_unico_vigente()
    {
        Pessoa pessoa = Visitante(Hoje.AddDays(-90));

        pessoa.RegistrarApresentacao(Hoje.AddDays(-60), Hoje);
        pessoa.ReconhecerAfastamento("Mudou de cidade", Hoje.AddDays(-30));
        pessoa.RegistrarApresentacao(Hoje, Hoje);

        Assert.Equal(4, pessoa.Vinculos.Count);
        Assert.Single(pessoa.Vinculos, vinculo => vinculo.DataFim is null);
    }

    [Fact]
    public void DataInicio_do_novo_deve_ser_a_DataFim_do_anterior()
    {
        Pessoa pessoa = Visitante(Hoje.AddDays(-30));

        pessoa.RegistrarApresentacao(Hoje, Hoje);

        Assert.Equal(
            Vinculo(pessoa, Situacao.Visitante).DataFim,
            Vinculo(pessoa, Situacao.Membro).DataInicio);
    }

    [Fact]
    public void Data_anterior_ao_inicio_do_vigente_deve_ser_recusada()
    {
        DateOnly chegada = Hoje.AddDays(-30);
        Pessoa pessoa = Visitante(chegada);

        Result resultado = pessoa.RegistrarApresentacao(chegada.AddDays(-1), Hoje);

        Assert.True(resultado.IsFailure);
        Assert.Equal("Pessoa.DataRetroativa", resultado.Error.Code);
    }

    [Fact]
    public void Data_igual_ao_inicio_do_vigente_deve_ser_aceita()
    {
        DateOnly chegada = Hoje.AddDays(-10);
        Pessoa pessoa = Visitante(chegada);

        Result resultado = pessoa.RegistrarApresentacao(chegada, Hoje);

        Assert.True(resultado.IsSuccess);
        Assert.Equal(chegada, Vinculo(pessoa, Situacao.Visitante).DataFim);
    }

    [Fact]
    public void Data_futura_deve_ser_recusada()
    {
        Pessoa pessoa = SemVinculo();

        Result resultado = pessoa.RegistrarApresentacao(Hoje.AddDays(1), Hoje);

        Assert.True(resultado.IsFailure);
        Assert.Equal("Pessoa.DataFutura", resultado.Error.Code);
        Assert.Empty(pessoa.Vinculos);
    }

    [Fact]
    public void Data_de_hoje_deve_ser_aceita()
    {
        Pessoa pessoa = SemVinculo();

        Result resultado = pessoa.RegistrarApresentacao(Hoje, Hoje);

        Assert.True(resultado.IsSuccess);
    }

    [Fact]
    public void Vinculo_encerrado_deve_preservar_a_situacao()
    {
        Pessoa pessoa = Visitante(Hoje.AddDays(-30));

        pessoa.RegistrarApresentacao(Hoje, Hoje);

        VinculoIgreja encerrado = Assert.Single(pessoa.Vinculos, vinculo => vinculo.DataFim is not null);

        Assert.Equal(Situacao.Visitante, encerrado.Situacao);
    }

    [Fact]
    public void Recusa_nao_deve_alterar_a_colecao_de_vinculos()
    {
        DateOnly chegada = Hoje.AddDays(-30);
        Pessoa pessoa = Visitante(chegada);

        pessoa.RegistrarApresentacao(chegada.AddDays(-1), Hoje);

        VinculoIgreja vinculo = Assert.Single(pessoa.Vinculos);

        Assert.Equal(chegada, vinculo.DataInicio);
        Assert.Null(vinculo.DataFim);
    }

    private static Pessoa SemVinculo() =>
        new(new PessoaId(Guid.CreateVersion7()), "João Guedes");

    private static Pessoa Visitante(DateOnly chegada) =>
        Pessoa.Cadastrar(
            hoje: chegada,
            nome: "João Guedes",
            convidadoPorId: null,
            celular: null,
            email: null,
            dataNascimento: null,
            estadoCivil: null,
            dataCasamento: null,
            profissao: null,
            dataBatismo: null,
            endereco: null).Value;

    private static VinculoIgreja Vinculo(Pessoa pessoa, Situacao situacao) =>
        pessoa.Vinculos.Single(vinculo => vinculo.Situacao == situacao);
}
