using CathedrAll.Kernel.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CathedrAll.Kernel.Web.Tests;

public sealed class EndpointResultsTests
{
    private static readonly Error Recusa = Error.NotFound(
        "Pessoa.NotFound",
        "Pessoa não encontrada.");

    [Fact]
    public void ToOk_deve_devolver_200_com_o_valor()
    {
        Ok<string> ok = Assert.IsType<Ok<string>>(Result.Success("João Guedes").ToOk().Result);

        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        Assert.Equal("João Guedes", ok.Value);
    }

    [Fact]
    public void ToOk_deve_devolver_o_problem_do_erro()
    {
        ProblemHttpResult problem = Assert.IsType<ProblemHttpResult>(
            Result.Failure<string>(Recusa).ToOk().Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal(Recusa.Code, problem.ProblemDetails.Extensions["code"]);
    }

    [Fact]
    public void ToNoContent_deve_devolver_204()
    {
        NoContent noContent = Assert.IsType<NoContent>(Result.Success().ToNoContent().Result);

        Assert.Equal(StatusCodes.Status204NoContent, noContent.StatusCode);
    }

    [Fact]
    public void ToNoContent_deve_devolver_o_problem_do_erro()
    {
        ProblemHttpResult problem = Assert.IsType<ProblemHttpResult>(
            Result.Failure(Recusa).ToNoContent().Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }
}
