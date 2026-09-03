using CathedrAll.Pessoas.Domain;

namespace CathedrAll.Pessoas.Application;

internal sealed record PessoaEncontrada(
    Guid Id,
    string Nome,
    Situacao Situacao,
    DateOnly Desde,
    PessoaRef? ConvidadoPor);
