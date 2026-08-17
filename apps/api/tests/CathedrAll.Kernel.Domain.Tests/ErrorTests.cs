namespace CathedrAll.Kernel.Domain.Tests;

public sealed class ErrorTests
{
    private const string AnyCode = "Test.AnyError";

    private const string AnyDescription = "Qualquer erro de teste";

    [Fact]
    public void Erros_com_os_mesmos_valores_devem_ser_iguais()
    {
        var first = Error.NotFound(AnyCode, AnyDescription);
        var second = Error.NotFound(AnyCode, AnyDescription);

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.Failure)]
    public void Cada_factory_define_o_tipo_de_erro_corretamente(ErrorType expectedType)
    {
        Error error = expectedType switch
        {
            ErrorType.Validation => Error.Validation(AnyCode, AnyDescription),
            ErrorType.NotFound => Error.NotFound(AnyCode, AnyDescription),
            ErrorType.Conflict => Error.Conflict(AnyCode, AnyDescription),
            ErrorType.Failure => Error.Failure(AnyCode, AnyDescription),
            _ => throw new ArgumentOutOfRangeException(nameof(expectedType), expectedType, null)
        };

        Assert.Equal(expectedType, error.Type);
    }
}
