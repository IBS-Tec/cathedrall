namespace CathedrAll.Pessoas.Application;

internal sealed record EnderecoDaFicha(
    string? Cep,
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string Bairro,
    string? Cidade,
    string? Uf);
