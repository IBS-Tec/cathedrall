using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;
using CathedrAll.Pessoas.Application;
using CathedrAll.Pessoas.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CathedrAll.Pessoas;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPessoasDbContext(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configure)
    {
        services.AddDbContext<PessoasDbContext>(options =>
        {
            configure(options);
            options.UseSnakeCaseNamingConvention();
        });

        return services;
    }

    public static IServiceCollection AddPessoasHandlers(this IServiceCollection services)
    {
        services.TryAddScoped<
            IRequestHandler<SearchPessoasQuery, Result<SearchPessoasResponse>>,
            SearchPessoasHandler>();

        services.TryAddScoped<
            IRequestHandler<ListPessoasQuery, Result<ListPessoasResponse>>,
            ListPessoasHandler>();

        services.TryAddScoped<
            IRequestHandler<ListAniversariantesQuery, Result<ListAniversariantesResponse>>,
            ListAniversariantesHandler>();

        services.TryAddScoped<
            IRequestHandler<GetFichaPessoaQuery, Result<FichaPessoa>>,
            GetFichaPessoaHandler>();

        return services;
    }

    public static IServiceCollection AddPessoasTransactionBehavior(
        this IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped(
                typeof(IPipelineBehavior<,>),
                typeof(PessoasTransactionBehavior<,>)));

        return services;
    }
}
