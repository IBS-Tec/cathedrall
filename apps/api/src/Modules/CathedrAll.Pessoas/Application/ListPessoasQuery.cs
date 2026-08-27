using CathedrAll.Kernel.Application;
using CathedrAll.Pessoas.Domain;

namespace CathedrAll.Pessoas.Application;

internal sealed record ListPessoasQuery(
    string? Term,
    Situacao? Situacao,
    string? Bairro,
    int? Page,
    int? Size) : IQuery<ListPessoasResponse>;
