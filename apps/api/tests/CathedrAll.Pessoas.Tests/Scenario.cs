using CathedrAll.Pessoas.Domain;
using CathedrAll.Pessoas.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Pessoas.Tests;

internal static class Scenario
{
    public static IRelationalModel ModeloRelacional()
    {
        ServiceCollection services = new();
        services.AddPessoasModule(options =>
            options.UseNpgsql("Host=modelo;Database=modelo"));

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        return scope.ServiceProvider
            .GetRequiredService<PessoasDbContext>()
            .Model
            .GetRelationalModel();
    }

    public static async Task<SqliteConnection> AbrirAsync()
    {
        SqliteConnection connection = new("Filename=:memory:");
        await connection.OpenAsync();

        await using ServiceProvider provider = Provedor(connection);
        using IServiceScope scope = provider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<PessoasDbContext>()
            .Database
            .EnsureCreatedAsync();

        return connection;
    }

    public static ServiceProvider Provedor(SqliteConnection connection)
    {
        ServiceCollection services = new();
        services.AddPessoasModule(options => options.UseSqlite(connection));

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    public static async Task<PessoaId> GravarAsync(ServiceProvider provider, Pessoa pessoa)
    {
        using IServiceScope scope = provider.CreateScope();
        PessoasDbContext context = scope.ServiceProvider.GetRequiredService<PessoasDbContext>();

        context.Pessoas.Add(pessoa);
        await context.SaveChangesAsync();

        return pessoa.Id;
    }

    public static async Task GravarComVinculoAsync(ServiceProvider provider, PessoaId id)
    {
        using IServiceScope scope = provider.CreateScope();
        PessoasDbContext context = scope.ServiceProvider.GetRequiredService<PessoasDbContext>();

        context.Pessoas.Add(new Pessoa(id, "João Guedes"));
        context.Add(new VinculoIgreja(
            new VinculoIgrejaId(Guid.CreateVersion7()),
            id,
            Situacao.Visitante,
            new DateOnly(2026, 8, 23)));

        await context.SaveChangesAsync();
    }

    public static ITable Tabela(string nome) =>
        ModeloRelacional().Tables.Single(tabela => tabela.Name == nome);

    public static string[] Colunas(ITable tabela) =>
        [.. tabela.Columns
            .Select(coluna => $"{coluna.Name} {coluna.StoreType}{(coluna.IsNullable ? string.Empty : " NOT NULL")}")
            .Order(StringComparer.Ordinal)];
}
