using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Kernel.Modules;

/// <summary>
/// The only surface a module exposes to the host.
///
/// The host knows how to register and map a module — nothing else about it. Everything
/// inside a module stays internal to its assembly, which is what makes the boundary a
/// compile-time guarantee instead of a convention (ADR-0012).
/// </summary>
public interface IModule
{
    string Name { get; }

    IServiceCollection Register(IServiceCollection services, IConfiguration configuration);

    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}
