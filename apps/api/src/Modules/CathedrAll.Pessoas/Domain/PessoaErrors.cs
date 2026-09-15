using CathedrAll.Kernel.Domain;

namespace CathedrAll.Pessoas.Domain;

internal static class PessoaErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Pessoa.NotFound",
        "Pessoa não encontrada.");

    public static readonly Error DataFutura = Error.Validation(
        "Pessoa.DataFutura",
        "A data não pode estar no futuro.");

    public static readonly Error DataRetroativa = Error.Validation(
        "Pessoa.DataRetroativa",
        "A data não pode ser anterior à situação atual da pessoa.");

    public static readonly Error TransicaoInvalida = Error.Conflict(
        "Pessoa.TransicaoInvalida",
        "Esta mudança de situação não é permitida.");

    public static readonly Error MotivoObrigatorio = Error.Validation(
        "Pessoa.MotivoObrigatorio",
        "O motivo é obrigatório.");
}
