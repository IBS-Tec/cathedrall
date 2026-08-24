using CathedrAll.Kernel.Domain;

namespace CathedrAll.Pessoas.Domain;

internal sealed class Pessoa : AggregateRoot<PessoaId>
{
    private readonly List<VinculoIgreja> _vinculos = [];

    internal Pessoa(
        PessoaId id,
        string nome)
        : base(id) => Nome = nome;

    public string Nome { get; private set; }

    public PessoaId? ConvidadoPorId { get; init; }

    public PessoaId? FundidaEmId { get; init; }

    public Celular? Celular { get; init; }

    public Email? Email { get; init; }

    public DateOnly? DataNascimento { get; init; }

    public EstadoCivil? EstadoCivil { get; init; }

    public DateOnly? DataCasamento { get; init; }

    public string? Profissao { get; init; }

    public DateOnly? DataBatismo { get; init; }

    public Endereco? Endereco { get; init; }

    public IReadOnlyList<VinculoIgreja> Vinculos => _vinculos.AsReadOnly();
}
