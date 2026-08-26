using CathedrAll.Kernel.Domain;

namespace CathedrAll.Pessoas.Domain;

internal sealed class Pessoa : AggregateRoot<PessoaId>
{
    private readonly List<VinculoIgreja> _vinculos = [];

    internal Pessoa(
        PessoaId id,
        string nome)
        : base(id)
    {
        Nome = nome;
        NomeNormalizado = TextNormalization.Normalize(nome);
    }

    public string Nome { get; private set; }

    public string NomeNormalizado { get; private set; }

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

    internal Result SucederVinculo(
        Situacao situacao,
        DateOnly data,
        string? motivo,
        DateOnly hoje)
    {
        if (data > hoje)
        {
            return Result.Failure(PessoaErrors.DataFutura);
        }

        VinculoIgreja? vigente = _vinculos.SingleOrDefault(v => v.DataFim is null);

        if (vigente is not null && data < vigente.DataInicio)
        {
            return Result.Failure(PessoaErrors.DataRetroativa);
        }

        vigente?.Encerrar(data);
        _vinculos.Add(VinculoIgreja.Abrir(Id, situacao, data, motivo));
        return Result.Success();
    }
}
