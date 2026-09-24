using CathedrAll.Kernel.Domain;
using CathedrAll.Pessoas.Domain;

namespace CathedrAll.Pessoas.Tests;

// Um teste por regra da seção 5 da spec. A varredura exaustiva da matriz, célula por célula,
// está em MatrizDeTransicoesTests.
public sealed class TransicoesDeVinculoTests
{
    private static readonly DateOnly Hoje = new(2026, 8, 25);
    private static readonly DateOnly Chegada = Hoje.AddDays(-90);
    private static readonly DateOnly Apresentacao = Hoje.AddDays(-60);
    private static readonly DateOnly Saida = Hoje.AddDays(-30);

    [Fact]
    public void Cadastro_sem_informar_situacao_deve_abrir_Visitante_com_a_data_de_hoje()
    {
        Pessoa pessoa = Cadastrar(comoMembro: false);

        VinculoIgreja vinculo = Assert.Single(pessoa.Vinculos);

        Assert.Equal(Situacao.Visitante, vinculo.Situacao);
        Assert.Equal(Hoje, vinculo.DataInicio);
    }

    [Fact]
    public void Cadastro_como_membro_deve_abrir_Membro_sem_passar_por_Visitante()
    {
        Pessoa pessoa = Cadastrar(comoMembro: true);

        Assert.Equal(Situacao.Membro, Assert.Single(pessoa.Vinculos).Situacao);
    }

    [Fact]
    public void Apresentacao_deve_abrir_Membro_na_data_da_cerimonia()
    {
        Pessoa pessoa = Visitante();

        Result resultado = pessoa.RegistrarApresentacao(Apresentacao, Hoje);

        Assert.True(resultado.IsSuccess);
        Assert.Equal(Situacao.Membro, Vigente(pessoa).Situacao);
        Assert.Equal(Apresentacao, Vigente(pessoa).DataInicio);
    }

    [Fact]
    public void Apresentacao_deve_valer_sem_vinculo() =>
        Assert.True(SemVinculo().RegistrarApresentacao(Hoje, Hoje).IsSuccess);

    [Fact]
    public void Apresentacao_deve_valer_de_Afastado() =>
        Assert.True(Afastado().RegistrarApresentacao(Hoje, Hoje).IsSuccess);

    [Fact]
    public void Apresentacao_deve_valer_de_Transferido() =>
        Assert.True(Transferido().RegistrarApresentacao(Hoje, Hoje).IsSuccess);

    [Fact]
    public void Apresentacao_de_quem_ja_e_Membro_deve_ser_recusada() =>
        DeveSerTransicaoInvalida(Membro(), pessoa => pessoa.RegistrarApresentacao(Hoje, Hoje));

    [Fact]
    public void Afastamento_deve_abrir_Afastado_no_dia_do_reconhecimento_com_o_motivo()
    {
        Pessoa pessoa = Membro();

        Result resultado = pessoa.ReconhecerAfastamento("  Mudou de cidade  ", Hoje);

        Assert.True(resultado.IsSuccess);
        Assert.Equal(Situacao.Afastado, Vigente(pessoa).Situacao);
        Assert.Equal(Hoje, Vigente(pessoa).DataInicio);
        Assert.Equal("Mudou de cidade", Vigente(pessoa).Motivo);
    }

    [Fact]
    public void Afastamento_de_Visitante_deve_ser_recusado() =>
        DeveSerTransicaoInvalida(Visitante(), pessoa => pessoa.ReconhecerAfastamento("Sumiu", Hoje));

    [Fact]
    public void Afastamento_sem_vinculo_deve_ser_recusado() =>
        DeveSerTransicaoInvalida(SemVinculo(), pessoa => pessoa.ReconhecerAfastamento("Sumiu", Hoje));

    [Fact]
    public void Afastamento_de_quem_ja_e_Afastado_deve_ser_recusado() =>
        DeveSerTransicaoInvalida(Afastado(), pessoa => pessoa.ReconhecerAfastamento("Sumiu", Hoje));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Afastamento_sem_motivo_deve_ser_recusado(string motivo)
    {
        Pessoa pessoa = Membro();

        Result resultado = pessoa.ReconhecerAfastamento(motivo, Hoje);

        Assert.Equal("Pessoa.MotivoObrigatorio", resultado.Error.Code);
        Assert.Equal(Situacao.Membro, Vigente(pessoa).Situacao);
    }

    [Fact]
    public void Transferencia_deve_abrir_Transferido_com_o_destino_no_motivo()
    {
        Pessoa pessoa = Membro();

        Result resultado = pessoa.RegistrarTransferencia("  Igreja Batista de Olinda  ", Saida, Hoje);

        Assert.True(resultado.IsSuccess);
        Assert.Equal(Situacao.Transferido, Vigente(pessoa).Situacao);
        Assert.Equal(Saida, Vigente(pessoa).DataInicio);
        Assert.Equal("Igreja Batista de Olinda", Vigente(pessoa).Motivo);
    }

    [Fact]
    public void Transferencia_deve_valer_de_Afastado() =>
        Assert.True(Afastado().RegistrarTransferencia("Igreja Batista de Olinda", Hoje, Hoje).IsSuccess);

    [Fact]
    public void Transferencia_de_Visitante_deve_ser_recusada() =>
        DeveSerTransicaoInvalida(
            Visitante(),
            pessoa => pessoa.RegistrarTransferencia("Igreja Batista de Olinda", Hoje, Hoje));

    [Fact]
    public void Transferencia_de_quem_ja_e_Transferido_deve_ser_recusada() =>
        DeveSerTransicaoInvalida(
            Transferido(),
            pessoa => pessoa.RegistrarTransferencia("Igreja Batista de Olinda", Hoje, Hoje));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Transferencia_sem_destino_deve_ser_recusada_com_MotivoObrigatorio(string destino)
    {
        Pessoa pessoa = Membro();

        Result resultado = pessoa.RegistrarTransferencia(destino, Hoje, Hoje);

        Assert.Equal("Pessoa.MotivoObrigatorio", resultado.Error.Code);
        Assert.Equal(Situacao.Membro, Vigente(pessoa).Situacao);
    }

    // O contrário da RN-7, de propósito: aqui o fato é conhecido e só a notícia chegou tarde.
    [Fact]
    public void Falecimento_deve_aceitar_data_retroativa()
    {
        Pessoa pessoa = Membro();

        Result resultado = pessoa.RegistrarFalecimento(Saida, Hoje);

        Assert.True(resultado.IsSuccess);
        Assert.Equal(Situacao.Falecido, Vigente(pessoa).Situacao);
        Assert.Equal(Saida, Vigente(pessoa).DataInicio);
    }

    [Fact]
    public void Falecimento_deve_valer_de_Visitante() =>
        Assert.True(Visitante().RegistrarFalecimento(Hoje, Hoje).IsSuccess);

    [Fact]
    public void Falecimento_deve_valer_de_Transferido() =>
        Assert.True(Transferido().RegistrarFalecimento(Hoje, Hoje).IsSuccess);

    [Fact]
    public void Falecimento_sem_vinculo_deve_ser_recusado() =>
        DeveSerTransicaoInvalida(SemVinculo(), pessoa => pessoa.RegistrarFalecimento(Hoje, Hoje));

    [Fact]
    public void Nenhuma_transicao_deve_partir_de_Falecido()
    {
        DeveSerTransicaoInvalida(Falecido(), pessoa => pessoa.RegistrarApresentacao(Hoje, Hoje));
        DeveSerTransicaoInvalida(Falecido(), pessoa => pessoa.ReconhecerAfastamento("Sumiu", Hoje));
        DeveSerTransicaoInvalida(
            Falecido(),
            pessoa => pessoa.RegistrarTransferencia("Igreja Batista de Olinda", Hoje, Hoje));
        DeveSerTransicaoInvalida(Falecido(), pessoa => pessoa.RegistrarFalecimento(Hoje, Hoje));
    }

    [Fact]
    public void Transicao_fora_da_matriz_deve_ser_Conflict_e_nao_mexer_no_historico()
    {
        Pessoa pessoa = Visitante();

        Result resultado = pessoa.ReconhecerAfastamento("Sumiu", Hoje);

        Assert.Equal(ErrorType.Conflict, resultado.Error.Type);

        VinculoIgreja vinculo = Assert.Single(pessoa.Vinculos);

        Assert.Equal(Situacao.Visitante, vinculo.Situacao);
        Assert.Null(vinculo.DataFim);
    }

    private static void DeveSerTransicaoInvalida(Pessoa pessoa, Func<Pessoa, Result> transicao)
    {
        int antes = pessoa.Vinculos.Count;

        Result resultado = transicao(pessoa);

        Assert.Equal("Pessoa.TransicaoInvalida", resultado.Error.Code);
        Assert.Equal(antes, pessoa.Vinculos.Count);
    }

    private static VinculoIgreja Vigente(Pessoa pessoa) =>
        pessoa.Vinculos.Single(vinculo => vinculo.DataFim is null);

    private static Pessoa SemVinculo() =>
        new(new PessoaId(Guid.CreateVersion7()), "João Guedes");

    private static Pessoa Cadastrar(bool comoMembro, DateOnly? hoje = null) =>
        Pessoa.Cadastrar(
            hoje: hoje ?? Hoje,
            nome: "João Guedes",
            convidadoPorId: null,
            celular: null,
            email: null,
            dataNascimento: null,
            estadoCivil: null,
            dataCasamento: null,
            profissao: null,
            dataBatismo: null,
            endereco: null,
            comoMembro: comoMembro).Value;

    private static Pessoa Visitante() => Cadastrar(comoMembro: false, hoje: Chegada);

    private static Pessoa Membro()
    {
        Pessoa pessoa = Visitante();
        pessoa.RegistrarApresentacao(Apresentacao, Apresentacao);

        return pessoa;
    }

    private static Pessoa Afastado()
    {
        Pessoa pessoa = Membro();
        pessoa.ReconhecerAfastamento("Mudou de cidade", Saida);

        return pessoa;
    }

    private static Pessoa Transferido()
    {
        Pessoa pessoa = Membro();
        pessoa.RegistrarTransferencia("Igreja Batista de Olinda", Saida, Saida);

        return pessoa;
    }

    private static Pessoa Falecido()
    {
        Pessoa pessoa = Membro();
        pessoa.RegistrarFalecimento(Saida, Saida);

        return pessoa;
    }
}
