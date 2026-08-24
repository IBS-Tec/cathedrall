using CathedrAll.Kernel.Domain;

namespace CathedrAll.Pessoas.Domain;

internal sealed class VinculoIgreja : Entity<VinculoIgrejaId>
{
    internal VinculoIgreja(
        VinculoIgrejaId id,
        PessoaId pessoaId,
        Situacao situacao,
        DateOnly dataInicio)
        : base(id)
    {
        PessoaId = pessoaId;
        Situacao = situacao;
        DataInicio = dataInicio;
    }

    public PessoaId PessoaId { get; init; }

    public Situacao Situacao { get; init; }

    public DateOnly DataInicio { get; init; }

    public DateOnly? DataFim { get; init; }

    public string? Motivo { get; init; }
}
