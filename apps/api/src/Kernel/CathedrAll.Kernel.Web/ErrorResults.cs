using CathedrAll.Kernel.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CathedrAll.Kernel.Web;

public static class ErrorResults
{
    public static ProblemHttpResult ToProblem(this Error error)
    {
        if (error == Error.None)
        {
            throw new InvalidOperationException("Um resultado bem-sucedido não tem erro para mapear.");
        }

        int status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            _ => throw new ArgumentOutOfRangeException(
                nameof(error),
                error.Type,
                $"{nameof(ErrorType)} sem status HTTP correspondente."),
        };

        return TypedResults.Problem(
            detail: error.Description,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
