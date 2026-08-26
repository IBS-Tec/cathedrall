using System.Text;
using CathedrAll.Pessoas.Domain;

namespace CathedrAll.Pessoas.Tests;

public sealed class NomeNormalizadoTests
{
    [Theory]
    [InlineData("João Guedes", "JOAO GUEDES")]
    [InlineData("joão guedes", "JOAO GUEDES")]
    [InlineData("JOÃO GUEDES", "JOAO GUEDES")]
    [InlineData("  João Guedes  ", "JOAO GUEDES")]
    [InlineData("Conceição", "CONCEICAO")]
    [InlineData("Muñoz", "MUNOZ")]
    [InlineData("Ana", "ANA")]
    public void Deve_normalizar_acento_caixa_e_espaco(string nome, string esperado) =>
        Assert.Equal(esperado, TextNormalization.Normalize(nome));

    [Fact]
    public void Nome_composto_e_decomposto_devem_normalizar_igual()
    {
        string composto = "João".Normalize(NormalizationForm.FormC);
        string decomposto = "João".Normalize(NormalizationForm.FormD);

        Assert.NotEqual(composto, decomposto, StringComparer.Ordinal);

        Assert.Equal(
            TextNormalization.Normalize(composto),
            TextNormalization.Normalize(decomposto));
    }

    [Fact]
    public void Normalizar_o_que_ja_esta_normalizado_nao_deve_mudar_nada()
    {
        string normalizado = TextNormalization.Normalize("João Guedes");

        Assert.Equal(normalizado, TextNormalization.Normalize(normalizado));
    }

    [Fact]
    public void Normalizado_nao_deve_exceder_o_tamanho_da_coluna()
    {
        string nome = new('ã', 120);

        Assert.Equal(120, TextNormalization.Normalize(nome).Length);
    }

    [Fact]
    public void Pessoa_deve_nascer_com_o_nome_normalizado()
    {
        Pessoa pessoa = new(new PessoaId(Guid.CreateVersion7()), "João Guedes");

        Assert.Equal("João Guedes", pessoa.Nome);
        Assert.Equal("JOAO GUEDES", pessoa.NomeNormalizado);
    }
}
