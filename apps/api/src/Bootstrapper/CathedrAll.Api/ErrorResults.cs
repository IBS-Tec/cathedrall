using CathedrAll.Kernel.Domain;

namespace CathedrAll.Api;

internal static class ErrorResults
{
    internal static IResult ToProblem(this Error error)
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
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            _ => throw new ArgumentOutOfRangeException(
                nameof(error),
                error.Type,
                $"{nameof(ErrorType)} sem status HTTP correspondente."),
        };

        return Results.Problem(
            detail: error.Description,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
