using CathedrAll.Pessoas.Domain;

namespace CathedrAll.Pessoas.Application;

internal sealed record PessoaDaLista(
    Guid Id,
    string Nome,
    Situacao Situacao,
    DateOnly Desde,
    string? Bairro);
