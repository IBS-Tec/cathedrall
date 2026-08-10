using CathedrAll.Kernel.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Pessoas;

/// <summary>
/// Módulo de cadastro. `Pessoa` é a raiz única: membro e visitante são situação de
/// vínculo, trabalhador é consulta por alocação ativa (ADR-0008).
///
/// Ainda sem entidades de propósito — o modelo em docs/dominio.md é rascunho e precisa
/// ser validado com a secretaria antes de virar migration.
/// </summary>
public sealed class PessoasModule : IModule
{
    public string Name => "Pessoas";

    public IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // TODO: .RequireAuthorization() assim que o RBAC com escopo existir. Sem ele
        // registrado o startup falha, e endpoint de pessoa sem autorização não pode
        // sequer existir temporariamente — é dado pessoal sensível.
        endpoints
            .MapGroup("/api/pessoas")
            .WithTags("Pessoas");

        return endpoints;
    }
}
