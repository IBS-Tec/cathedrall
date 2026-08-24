using CathedrAll.Pessoas.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Pessoas;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPessoasModule(
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
}
