using CathedrAll.Kernel.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CathedrAll.Kernel.Web;

public static class EndpointResults
{
    public static Results<Ok<TValue>, ProblemHttpResult> ToOk<TValue>(this Result<TValue> result) =>
        result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.ToProblem();

    public static Results<NoContent, ProblemHttpResult> ToNoContent(this Result result) =>
        result.IsSuccess
            ? TypedResults.NoContent()
            : result.Error.ToProblem();
}
