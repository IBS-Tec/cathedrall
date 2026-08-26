using CathedrAll.Pessoas;
using CathedrAll.Pessoas.Domain;
using CathedrAll.Pessoas.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Api.Tests;

internal sealed class PessoasApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Filename=:memory:");

    internal async Task<HttpClient> SemearAsync(params Pessoa[] pessoas)
    {
        HttpClient client = CreateClient();

        using IServiceScope scope = Services.CreateScope();
        PessoasDbContext context = scope.ServiceProvider.GetRequiredService<PessoasDbContext>();

        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        context.Pessoas.AddRange(pessoas);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            ServiceDescriptor[] doPostgres = [.. services.Where(ApontaParaPessoas)];

            foreach (ServiceDescriptor descriptor in doPostgres)
            {
                services.Remove(descriptor);
            }

            services.AddPessoasDbContext(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }

    private static bool ApontaParaPessoas(ServiceDescriptor descriptor)
    {
        if (descriptor.ServiceType == typeof(PessoasDbContext))
        {
            return true;
        }

        return descriptor.ServiceType.IsGenericType
            && descriptor.ServiceType.GetGenericArguments().Contains(typeof(PessoasDbContext));
    }
}
