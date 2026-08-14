namespace CathedrAll.Kernel.Domain.Tests;

public sealed class ErrorTests
{
    [Fact]
    public void Erros_com_os_mesmos_valores_devem_ser_iguais()
    {
        var erro1 = Error.NotFound("Qualquer.Erro", "Qualquer erro de teste");
        var erro2 = Error.NotFound("Qualquer.Erro", "Qualquer erro de teste");

        Assert.Equal(erro1, erro2);
    }

    [Theory]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.Failure)]
    public void Cada_factory_define_o_tipo_de_erro_corretamente(ErrorType tipoEsperado)
    {
        Error erro = tipoEsperado switch
        {
            ErrorType.Validation => Error.Validation("Qualquer.Erro", "Qualquer erro de teste"),
            ErrorType.NotFound => Error.NotFound("Qualquer.Erro", "Qualquer erro de teste"),
            ErrorType.Conflict => Error.Conflict("Qualquer.Erro", "Qualquer erro de teste"),
            ErrorType.Failure => Error.Failure("Qualquer.Erro", "Qualquer erro de teste"),
            _ => throw new ArgumentOutOfRangeException(nameof(tipoEsperado), tipoEsperado, null)
        };

        Assert.Equal(tipoEsperado, erro.Type);
    }
}
