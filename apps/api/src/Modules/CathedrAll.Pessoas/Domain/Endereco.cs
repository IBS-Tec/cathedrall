namespace CathedrAll.Pessoas.Domain;

internal sealed record Endereco(
    string? Cep,
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string Bairro,
    string? Cidade,
    string? Uf)
{
    public string Bairro { get; } = Bairro.Trim();

    public string BairroNormalizado { get; private set; } = TextNormalization.Normalize(Bairro);
}
