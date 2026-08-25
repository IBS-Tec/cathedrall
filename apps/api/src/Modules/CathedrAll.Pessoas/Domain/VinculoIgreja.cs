using CathedrAll.Kernel.Domain;

namespace CathedrAll.Pessoas.Domain;

internal sealed class VinculoIgreja : Entity<VinculoIgrejaId>
{
    private VinculoIgreja(
        VinculoIgrejaId id,
        PessoaId pessoaId,
        Situacao situacao,
        DateOnly dataInicio,
        string? motivo)
        : base(id)
    {
        PessoaId = pessoaId;
        Situacao = situacao;
        DataInicio = dataInicio;
        Motivo = motivo;
    }

    public PessoaId PessoaId { get; init; }

    public Situacao Situacao { get; init; }

    public DateOnly DataInicio { get; init; }

    public DateOnly? DataFim { get; private set; }

    public string? Motivo { get; init; }

    internal static VinculoIgreja Abrir(
        PessoaId pessoaId,
        Situacao situacao,
        DateOnly data,
        string? motivo)
    {
        var id = new VinculoIgrejaId(Guid.CreateVersion7());
        return new(id, pessoaId, situacao, data, motivo);
    }

    internal void Encerrar(DateOnly dataEncerramento) => DataFim = dataEncerramento;
}
