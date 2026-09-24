using CathedrAll.Kernel.Domain;
using CathedrAll.Pessoas.Domain;

namespace CathedrAll.Pessoas.Tests;

// A matriz da seção 5 da spec, varrida inteira: cada par de situações mais as entradas de
// cadastro. Os casos saem de Enum.GetValues, e não de uma lista escrita à mão.
//
// Acrescentar um valor a Situacao deixa este teste VERMELHO, e isso é o desenho. Os casos da
// linha e da coluna novas aparecem sozinhos como "SEM DECISÃO", porque Matriz não tem braço
// padrão que diga Recusada. Quem acrescentou decide célula por célula, na seção 5 e aqui.
public sealed class MatrizDeTransicoesTests
{
    private const string Cadastro = "Cadastro";

    private static readonly DateOnly Hoje = new(2026, 8, 25);
    private static readonly DateOnly Chegada = Hoje.AddDays(-90);
    private static readonly DateOnly Apresentacao = Hoje.AddDays(-60);
    private static readonly DateOnly Saida = Hoje.AddDays(-30);
    private static readonly DateOnly Retroativa = Hoje.AddDays(-10);

    // Cada célula tem condição, não só permissão.
    private enum Celula
    {
        Recusada,
        Permitida,

        // RN-7 e RN-8: sem Motivo, Pessoa.MotivoObrigatorio.
        ExigeMotivo,

        // RN-9: o fato é conhecido e só a notícia chegou tarde.
        AceitaDataRetroativa
    }

    public static IEnumerable<TheoryDataRow<string, string>> Celulas()
    {
        Situacao[] situacoes = Enum.GetValues<Situacao>();

        foreach (Situacao para in situacoes)
        {
            yield return Caso(null, para);
        }

        foreach (Situacao de in situacoes)
        {
            foreach (Situacao para in situacoes)
            {
                yield return Caso(de, para);
            }
        }
    }

    [Theory]
    [MemberData(nameof(Celulas))]
    public void Cada_celula_da_matriz_deve_ser_cumprida(string de, string para)
    {
        Situacao? origem = de == Cadastro ? null : Enum.Parse<Situacao>(de);
        Situacao destino = Enum.Parse<Situacao>(para);

        Celula? esperada = Matriz(origem, destino);

        Assert.True(
            esperada.HasValue,
            $"{de} → {para} não tem decisão. Decida a célula na seção 5 da spec e em Matriz.");

        if (origem is null)
        {
            DeveCumprirCadastro(destino, esperada.Value);
        }
        else
        {
            DeveCumprirTransicao(origem.Value, destino, esperada.Value);
        }
    }

    // A tabela da seção 5, célula por célula. Sem braço padrão Recusada, de propósito.
    private static Celula? Matriz(Situacao? de, Situacao para) => (de, para) switch
    {
        (null, Situacao.Visitante) => Celula.Permitida,
        (null, Situacao.Membro) => Celula.Permitida,
        (null, Situacao.Afastado) => Celula.Recusada,
        (null, Situacao.Transferido) => Celula.Recusada,
        (null, Situacao.Falecido) => Celula.Recusada,

        (Situacao.Visitante, Situacao.Visitante) => Celula.Recusada,
        (Situacao.Visitante, Situacao.Membro) => Celula.Permitida,
        (Situacao.Visitante, Situacao.Afastado) => Celula.Recusada,
        (Situacao.Visitante, Situacao.Transferido) => Celula.Recusada,
        (Situacao.Visitante, Situacao.Falecido) => Celula.AceitaDataRetroativa,

        (Situacao.Membro, Situacao.Visitante) => Celula.Recusada,
        (Situacao.Membro, Situacao.Membro) => Celula.Recusada,
        (Situacao.Membro, Situacao.Afastado) => Celula.ExigeMotivo,
        (Situacao.Membro, Situacao.Transferido) => Celula.ExigeMotivo,
        (Situacao.Membro, Situacao.Falecido) => Celula.AceitaDataRetroativa,

        (Situacao.Afastado, Situacao.Visitante) => Celula.Recusada,
        (Situacao.Afastado, Situacao.Membro) => Celula.Permitida,
        (Situacao.Afastado, Situacao.Afastado) => Celula.Recusada,
        (Situacao.Afastado, Situacao.Transferido) => Celula.ExigeMotivo,
        (Situacao.Afastado, Situacao.Falecido) => Celula.AceitaDataRetroativa,

        (Situacao.Transferido, Situacao.Visitante) => Celula.Recusada,
        (Situacao.Transferido, Situacao.Membro) => Celula.Permitida,
        (Situacao.Transferido, Situacao.Afastado) => Celula.Recusada,
        (Situacao.Transferido, Situacao.Transferido) => Celula.Recusada,
        (Situacao.Transferido, Situacao.Falecido) => Celula.AceitaDataRetroativa,

        (Situacao.Falecido, Situacao.Visitante) => Celula.Recusada,
        (Situacao.Falecido, Situacao.Membro) => Celula.Recusada,
        (Situacao.Falecido, Situacao.Afastado) => Celula.Recusada,
        (Situacao.Falecido, Situacao.Transferido) => Celula.Recusada,
        (Situacao.Falecido, Situacao.Falecido) => Celula.Recusada,

        _ => null
    };

    private static TheoryDataRow<string, string> Caso(Situacao? de, Situacao para)
    {
        string origem = de?.ToString() ?? Cadastro;

        string resultado = Matriz(de, para) switch
        {
            Celula.Recusada => "recusada",
            Celula.Permitida => "permitida",
            Celula.ExigeMotivo => "permitida, exige motivo",
            Celula.AceitaDataRetroativa => "permitida, aceita data retroativa",
            _ => "SEM DECISÃO"
        };

        return new TheoryDataRow<string, string>(origem, para.ToString())
        {
            TestDisplayName = $"Matriz: {origem} → {para} — {resultado} "
        };
    }

    // Cadastrar só sabe abrir Visitante ou Membro: as outras três entradas são recusadas pela
    // assinatura, e não por erro.
    private static void DeveCumprirCadastro(Situacao destino, Celula esperada)
    {
        bool? comoMembro = destino switch
        {
            Situacao.Visitante => false,
            Situacao.Membro => true,
            Situacao.Afastado or Situacao.Transferido or Situacao.Falecido => null,
            _ => throw new InvalidOperationException($"Cadastro não sabe tratar {destino}.")
        };

        if (comoMembro is null)
        {
            Assert.Equal(Celula.Recusada, esperada);
            return;
        }

        Assert.Equal(Celula.Permitida, esperada);

        Result<Pessoa> resultado = Cadastrar(Hoje, comoMembro.Value);

        Assert.True(resultado.IsSuccess);
        Assert.Equal(destino, Vigente(resultado.Value).Situacao);
    }

    private static void DeveCumprirTransicao(Situacao origem, Situacao destino, Celula esperada)
    {
        Func<Pessoa, string, DateOnly, Result>? ato = Ato(destino);

        // RN-11: nenhum ato leva a Visitante. A recusa está na assinatura.
        if (ato is null)
        {
            Assert.Equal(Celula.Recusada, esperada);
            return;
        }

        Pessoa pessoa = Em(origem);
        int antes = pessoa.Vinculos.Count;

        switch (esperada)
        {
            case Celula.Recusada:
                Assert.Equal(
                    "Pessoa.TransicaoInvalida",
                    ato(pessoa, "Mudou de cidade", Retroativa).Error.Code);
                Assert.Equal(antes, pessoa.Vinculos.Count);
                break;

            case Celula.Permitida:
                Assert.True(ato(pessoa, "Mudou de cidade", Retroativa).IsSuccess);
                Assert.Equal(destino, Vigente(pessoa).Situacao);
                break;

            case Celula.ExigeMotivo:
                Assert.Equal("Pessoa.MotivoObrigatorio", ato(pessoa, "   ", Retroativa).Error.Code);
                Assert.Equal(antes, pessoa.Vinculos.Count);

                Assert.True(ato(pessoa, "  Mudou de cidade  ", Retroativa).IsSuccess);
                Assert.Equal(destino, Vigente(pessoa).Situacao);
                Assert.Equal("Mudou de cidade", Vigente(pessoa).Motivo);
                break;

            case Celula.AceitaDataRetroativa:
                Assert.True(ato(pessoa, "Mudou de cidade", Retroativa).IsSuccess);
                Assert.Equal(destino, Vigente(pessoa).Situacao);
                Assert.Equal(Retroativa, Vigente(pessoa).DataInicio);
                break;

            default:
                throw new InvalidOperationException($"Célula sem verificação: {esperada}.");
        }
    }

    // O ato que leva a cada destino. Quem não usa motivo ou data ignora o parâmetro.
    private static Func<Pessoa, string, DateOnly, Result>? Ato(Situacao destino) => destino switch
    {
        Situacao.Visitante => null,
        Situacao.Membro => (pessoa, _, data) => pessoa.RegistrarApresentacao(data, Hoje),
        Situacao.Afastado => (pessoa, motivo, _) => pessoa.ReconhecerAfastamento(motivo, Hoje),
        Situacao.Transferido => (pessoa, motivo, data) => pessoa.RegistrarTransferencia(motivo, data, Hoje),
        Situacao.Falecido => (pessoa, _, data) => pessoa.RegistrarFalecimento(data, Hoje),
        _ => throw new InvalidOperationException($"Nenhum ato conhecido leva a {destino}.")
    };

    private static Pessoa Em(Situacao origem)
    {
        Pessoa pessoa = Cadastrar(Chegada, comoMembro: false).Value;

        if (origem == Situacao.Visitante)
        {
            return pessoa;
        }

        pessoa.RegistrarApresentacao(Apresentacao, Apresentacao);

        Result resultado = origem switch
        {
            Situacao.Membro => Result.Success(),
            Situacao.Afastado => pessoa.ReconhecerAfastamento("Parou de vir", Saida),
            Situacao.Transferido => pessoa.RegistrarTransferencia("Igreja Batista de Olinda", Saida, Saida),
            Situacao.Falecido => pessoa.RegistrarFalecimento(Saida, Saida),
            _ => throw new InvalidOperationException($"Não sei montar uma pessoa em {origem}.")
        };

        Assert.True(resultado.IsSuccess);
        Assert.Equal(origem, Vigente(pessoa).Situacao);

        return pessoa;
    }

    private static Result<Pessoa> Cadastrar(DateOnly hoje, bool comoMembro) =>
        Pessoa.Cadastrar(
            hoje: hoje,
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
            comoMembro: comoMembro);

    private static VinculoIgreja Vigente(Pessoa pessoa) =>
        pessoa.Vinculos.Single(vinculo => vinculo.DataFim is null);
}
