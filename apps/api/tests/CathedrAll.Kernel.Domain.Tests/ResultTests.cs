namespace CathedrAll.Kernel.Domain.Tests;

public sealed class ResultTests
{
    private static readonly Error AnyError = Error.NotFound("Test.AnyError", "Qualquer erro de teste");

    [Fact]
    public void Sucesso_nao_deve_ter_erro()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Falha_deve_ter_erro()
    {
        var result = Result.Failure(AnyError);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(AnyError, result.Error);
    }

    [Fact]
    public void Falha_sem_erro_deve_lancar_excecao() =>
        Assert.Throws<ArgumentException>(() => Result.Failure(Error.None));

    [Fact]
    public void Ler_valor_de_falha_deve_lancar_excecao()
    {
        var result = Result.Failure<string>(AnyError);

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Valor_convertido_implicitamente_deve_ser_sucesso()
    {
        Result<string> result = "Qualquer valor";

        Assert.True(result.IsSuccess);
        Assert.Equal("Qualquer valor", result.Value);
    }

    [Fact]
    public void Erro_convertido_implicitamente_deve_ser_falha()
    {
        Result<string> result = AnyError;

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(AnyError, result.Error);
    }
}
