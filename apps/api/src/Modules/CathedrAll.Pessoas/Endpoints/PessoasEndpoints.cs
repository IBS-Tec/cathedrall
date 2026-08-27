using CathedrAll.Kernel.Application;
using CathedrAll.Pessoas.Application;
using CathedrAll.Pessoas.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CathedrAll.Pessoas.Endpoints;

public static class PessoasEndpoints
{
    public static IEndpointRouteBuilder MapPessoasEndpoints(this IEndpointRouteBuilder builder)
    {
        RouteGroupBuilder pessoas = builder.MapGroup("/api/pessoas");

        pessoas.MapGet("/", ListAsync).WithName("ListPessoas");
        pessoas.MapGet("/search", SearchAsync).WithName("SearchPessoas");

        return builder;
    }

    private static async Task<Ok<ListPessoasResponse>> ListAsync(
        [FromQuery(Name = "q")] string? term,
        Situacao? situacao,
        string? bairro,
        int? page,
        int? size,
        ISender sender,
        CancellationToken cancellationToken)
    {
        ListPessoasResponse response = await sender
            .SendAsync<ListPessoasQuery, ListPessoasResponse>(
                new ListPessoasQuery(term, situacao, bairro, page, size),
                cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<SearchPessoasResponse>> SearchAsync(
        [FromQuery(Name = "q")] string? term,
        ISender sender,
        CancellationToken cancellationToken)
    {
        SearchPessoasResponse response = await sender
            .SendAsync<SearchPessoasQuery, SearchPessoasResponse>(
                new SearchPessoasQuery(term ?? string.Empty),
                cancellationToken);

        return TypedResults.Ok(response);
    }
}
