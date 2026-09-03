using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;
using CathedrAll.Kernel.Web;
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
        pessoas.MapGet("/aniversariantes", ListAniversariantesAsync).WithName("ListAniversariantes");
        pessoas.MapGet("/{id:guid}", GetFichaPessoaAsync).WithName("GetFichaPessoa");

        return builder;
    }

    private static async Task<Results<Ok<ListPessoasResponse>, ProblemHttpResult>> ListAsync(
        [FromQuery(Name = "q")] string? term,
        Situacao? situacao,
        string? bairro,
        int? page,
        int? size,
        ISender sender,
        CancellationToken cancellationToken)
    {
        Result<ListPessoasResponse> result = await sender
            .SendAsync<ListPessoasQuery, Result<ListPessoasResponse>>(
                new ListPessoasQuery(term, situacao, bairro, page, size),
                cancellationToken);

        return result.ToOk();
    }

    private static async Task<Results<Ok<SearchPessoasResponse>, ProblemHttpResult>> SearchAsync(
        [FromQuery(Name = "q")] string? term,
        ISender sender,
        CancellationToken cancellationToken)
    {
        Result<SearchPessoasResponse> result = await sender
            .SendAsync<SearchPessoasQuery, Result<SearchPessoasResponse>>(
                new SearchPessoasQuery(term ?? string.Empty),
                cancellationToken);

        return result.ToOk();
    }

    private static async Task<Results<Ok<ListAniversariantesResponse>, ProblemHttpResult>> ListAniversariantesAsync(
        DateOnly from,
        DateOnly to,
        ISender sender,
        CancellationToken cancellationToken)
    {
        Result<ListAniversariantesResponse> result = await sender
            .SendAsync<ListAniversariantesQuery, Result<ListAniversariantesResponse>>(
                new ListAniversariantesQuery(from, to),
                cancellationToken);

        return result.ToOk();
    }

    private static async Task<Results<Ok<FichaPessoa>, ProblemHttpResult>> GetFichaPessoaAsync(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        Result<FichaPessoa> result = await sender
            .SendAsync<GetFichaPessoaQuery, Result<FichaPessoa>>(
                new GetFichaPessoaQuery(id),
                cancellationToken);

        return result.ToOk();
    }
}
