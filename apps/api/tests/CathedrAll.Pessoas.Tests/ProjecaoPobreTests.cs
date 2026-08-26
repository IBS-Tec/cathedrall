using System.Reflection;
using CathedrAll.Pessoas.Application;

namespace CathedrAll.Pessoas.Tests;

public sealed class ProjecaoPobreTests
{
    [Fact]
    public void PessoaEncontrada_deve_ter_exatamente_os_campos_da_secao_6_da_spec() =>
        Assert.Equal(
            ["ConvidadoPor", "Desde", "Id", "Nome", "Situacao"],
            Propriedades(typeof(PessoaEncontrada)));

    [Fact]
    public void PessoaRef_deve_levar_so_ao_registro_do_convite() =>
        Assert.Equal(["Id", "Nome"], Propriedades(typeof(PessoaRef)));

    [Fact]
    public void SearchPessoasResponse_deve_ter_so_a_lista() =>
        Assert.Equal(["Results"], Propriedades(typeof(SearchPessoasResponse)));

    [Theory]
    [InlineData("Celular")]
    [InlineData("Email")]
    [InlineData("Endereco")]
    [InlineData("DataNascimento")]
    [InlineData("EstadoCivil")]
    [InlineData("DataCasamento")]
    [InlineData("Profissao")]
    [InlineData("DataBatismo")]
    public void Nenhum_campo_coletado_na_apresentacao_deve_chegar_a_recepcao(string campo)
    {
        Assert.DoesNotContain(campo, Propriedades(typeof(PessoaEncontrada)));
        Assert.DoesNotContain(campo, Propriedades(typeof(PessoaRef)));
    }

    private static string[] Propriedades(Type tipo) =>
        [.. tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(propriedade => propriedade.Name)
            .Order(StringComparer.Ordinal)];
}
