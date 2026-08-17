using CathedrAll.Kernel.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CathedrAll.Api.Tests;

public sealed class ErrorResultsTests
{
    private const string Code = "Pessoa.NotFound";

    private const string Description = "Pessoa não encontrada.";

    [Fact]
    public void Deve_mapear_Validation_para_400()
    {
        ProblemHttpResult problem =
            Mapear(Error.Validation("Pessoa.InvalidEmail", "E-mail em formato inválido."));

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public void Deve_mapear_NotFound_para_404()
    {
        ProblemHttpResult problem = Mapear(Error.NotFound(Code, Description));

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public void Deve_mapear_Conflict_para_409()
    {
        ProblemHttpResult problem = Mapear(
            Error.Conflict("Escala.PessoaUnavailable", "A pessoa está indisponível nesta data."));

        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
    }

    [Fact]
    public void Deve_mapear_Failure_para_500()
    {
        ProblemHttpResult problem =
            Mapear(Error.Failure("Pessoa.UnexpectedFailure", "Não foi possível concluir."));

        Assert.Equal(StatusCodes.Status500InternalServerError, problem.StatusCode);
    }

    [Fact]
    public void Deve_levar_o_Code_no_campo_code()
    {
        ProblemHttpResult problem = Mapear(Error.NotFound(Code, Description));

        Assert.Equal(Code, problem.ProblemDetails.Extensions["code"]);
    }

    [Fact]
    public void Deve_levar_a_Description_no_detail()
    {
        ProblemHttpResult problem = Mapear(Error.NotFound(Code, Description));

        Assert.Equal(Description, problem.ProblemDetails.Detail);
    }

    [Fact]
    public void Nao_deve_levar_a_Description_no_title()
    {
        ProblemHttpResult problem = Mapear(Error.NotFound(Code, Description));

        Assert.NotEqual(Description, problem.ProblemDetails.Title);
    }

    [Fact]
    public void Deve_usar_o_titulo_generico_do_status()
    {
        ProblemHttpResult problem = Mapear(Error.NotFound(Code, Description));

        Assert.Equal("Not Found", problem.ProblemDetails.Title);
    }

    [Fact]
    public void Deve_lancar_ao_mapear_Error_None() =>
        Assert.Throws<InvalidOperationException>(() => Error.None.ToProblem());

    private static ProblemHttpResult Mapear(Error error) =>
        Assert.IsType<ProblemHttpResult>(error.ToProblem());
}
