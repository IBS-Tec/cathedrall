namespace CathedrAll.Kernel.Domain.Tests;

public sealed class ResultTests
{
    private static readonly Error QualquerErro = Error.NotFound("Qualquer.Erro", "Qualquer erro de teste");

    [Fact]
    public void Sucesso_nao_deve_ter_erro()
    {
        var resultado = Result.Success();

        Assert.True(resultado.IsSuccess);
        Assert.False(resultado.IsFailure);
        Assert.Equal(Error.None, resultado.Error);
    }

    [Fact]
    public void Falha_deve_ter_erro()
    {
        var resultado = Result.Failure(QualquerErro);

        Assert.False(resultado.IsSuccess);
        Assert.True(resultado.IsFailure);
        Assert.Equal(QualquerErro, resultado.Error);
    }

    [Fact]
    public void Falha_sem_erro_deve_lancar_excecao() =>
        Assert.Throws<ArgumentException>(() => Result.Failure(Error.None));

    [Fact]
    public void Ler_valor_de_falha_deve_lancar_excecao()
    {
        var resultado = Result.Failure<string>(QualquerErro);

        Assert.Throws<InvalidOperationException>(() => resultado.Value);
    }

    [Fact]
    public void Valor_convertido_implicitamente_deve_ser_sucesso()
    {
        Result<string> resultado = "Qualquer valor";

        Assert.True(resultado.IsSuccess);
        Assert.Equal("Qualquer valor", resultado.Value);
    }

    [Fact]
    public void Erro_convertido_implicitamente_deve_ser_falha()
    {
        Result<string> resultado = QualquerErro;

        Assert.False(resultado.IsSuccess);
        Assert.True(resultado.IsFailure);
        Assert.Equal(QualquerErro, resultado.Error);
    }
}
