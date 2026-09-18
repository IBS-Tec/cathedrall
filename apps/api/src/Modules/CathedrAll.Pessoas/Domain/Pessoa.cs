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

    public bool Anonimizada { get; private set; }

    public Celular? Celular { get; private set; }

    public Email? Email { get; private set; }

    public DateOnly? DataNascimento { get; private set; }

    public EstadoCivil? EstadoCivil { get; private set; }

    public DateOnly? DataCasamento { get; private set; }

    public string? Profissao { get; private set; }

    public DateOnly? DataBatismo { get; private set; }

    public Endereco? Endereco { get; private set; }

    public IReadOnlyList<VinculoIgreja> Vinculos => _vinculos.AsReadOnly();

    private Situacao? SituacaoAtual => _vinculos.SingleOrDefault(v => v.DataFim is null)?.Situacao;

    internal static Result<Pessoa> Cadastrar(
        DateOnly hoje,
        string nome,
        PessoaId? convidadoPorId,
        Celular? celular,
        Email? email,
        DateOnly? dataNascimento,
        EstadoCivil? estadoCivil,
        DateOnly? dataCasamento,
        string? profissao,
        DateOnly? dataBatismo,
        Endereco? endereco,
        bool comoMembro = false)
    {
        var id = new PessoaId(Guid.CreateVersion7());

        var pessoa = new Pessoa(id, nome)
        {
            ConvidadoPorId = convidadoPorId,
            Celular = celular,
            Email = email,
            DataNascimento = dataNascimento,
            EstadoCivil = estadoCivil,
            DataCasamento = dataCasamento,
            Profissao = profissao,
            DataBatismo = dataBatismo,
            Endereco = endereco
        };

        Situacao situacao = Situacao.Visitante;

        if (comoMembro)
        {
            situacao = Situacao.Membro;
        }

        Result result = pessoa.SucederVinculo(situacao, hoje, null, hoje);

        if (result.IsFailure)
        {
            return result.Error;
        }

        return pessoa;
    }

    internal Result RegistrarApresentacao(DateOnly data, DateOnly hoje)
    {
        if (SituacaoAtual is not null &&
            SituacaoAtual != Situacao.Visitante &&
            SituacaoAtual != Situacao.Afastado &&
            SituacaoAtual != Situacao.Transferido)
        {
            return Result.Failure(PessoaErrors.TransicaoInvalida);
        }

        return SucederVinculo(Situacao.Membro, data, null, hoje);
    }

    internal Result ReconhecerAfastamento(string motivo, DateOnly hoje)
    {
        if (SituacaoAtual != Situacao.Membro)
        {
            return Result.Failure(PessoaErrors.TransicaoInvalida);
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            return Result.Failure(PessoaErrors.MotivoObrigatorio);
        }

        return SucederVinculo(Situacao.Afastado, hoje, motivo.Trim(), hoje);
    }

    internal Result RegistrarTransferencia(string destino, DateOnly data, DateOnly hoje)
    {
        if (SituacaoAtual != Situacao.Membro &&
            SituacaoAtual != Situacao.Afastado)
        {
            return Result.Failure(PessoaErrors.TransicaoInvalida);
        }

        if (string.IsNullOrWhiteSpace(destino))
        {
            return Result.Failure(PessoaErrors.MotivoObrigatorio);
        }

        return SucederVinculo(Situacao.Transferido, data, destino.Trim(), hoje);
    }

    internal Result RegistrarFalecimento(DateOnly data, DateOnly hoje)
    {
        if (SituacaoAtual != Situacao.Visitante &&
            SituacaoAtual != Situacao.Membro &&
            SituacaoAtual != Situacao.Afastado &&
            SituacaoAtual != Situacao.Transferido)
        {
            return Result.Failure(PessoaErrors.TransicaoInvalida);
        }

        return SucederVinculo(Situacao.Falecido, data, null, hoje);
    }

    internal Result Anonimizar()
    {
        if (Anonimizada)
        {
            return Result.Failure(PessoaErrors.Anonimizada);
        }

        const string Anonimizado = "ANONIMIZADO";

        Nome = Anonimizado;
        NomeNormalizado = Anonimizado;
        Celular = null;
        Email = null;
        DataNascimento = null;
        EstadoCivil = null;
        DataCasamento = null;
        Profissao = null;
        DataBatismo = null;
        Endereco = null;
        Anonimizada = true;

        return Result.Success();
    }

    private Result SucederVinculo(
        Situacao situacao,
        DateOnly data,
        string? motivo,
        DateOnly hoje)
    {
        if (Anonimizada)
        {
            return Result.Failure(PessoaErrors.Anonimizada);
        }

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
