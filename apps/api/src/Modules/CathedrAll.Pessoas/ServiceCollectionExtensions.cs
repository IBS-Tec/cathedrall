using CathedrAll.Kernel.Application;
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
            IRequestHandler<SearchPessoasQuery, SearchPessoasResponse>,
            SearchPessoasHandler>();

        services.TryAddScoped<
            IRequestHandler<ListPessoasQuery, ListPessoasResponse>,
            ListPessoasHandler>();

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
