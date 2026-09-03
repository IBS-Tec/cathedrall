using CathedrAll.Pessoas.Domain;

namespace CathedrAll.Pessoas.Application;

internal sealed record FichaPessoa(
    Guid Id,
    string Nome,
    Situacao Situacao,
    PessoaRef? ConvidadoPor,
    string? Celular,
    string? Email,
    DateOnly? DataNascimento,
    EstadoCivil? EstadoCivil,
    DateOnly? DataCasamento,
    EnderecoDaFicha? Endereco,
    string? Profissao,
    DateOnly? DataBatismo,
    IReadOnlyList<VinculoDaFicha> Vinculos,
    PessoaRef? FundidaEm,
    bool Anonimizada);
