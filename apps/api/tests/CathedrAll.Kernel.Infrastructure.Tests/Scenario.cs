using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Kernel.Infrastructure.Tests;

internal static class Scenario
{
    public static async Task<SqliteConnection> OpenAsync()
    {
        SqliteConnection connection = new("Filename=:memory:");
        await connection.OpenAsync();

        await using FakeDbContext context = new(OptionsFor(connection));
        await context.Database.EnsureCreatedAsync();

        return connection;
    }

    public static ServiceProvider Build(
        SqliteConnection connection,
        Action<IServiceCollection> register)
    {
        ServiceCollection services = new();

        services.AddKernelApplication();
        services.AddDbContext<FakeDbContext>(options => options.UseSqlite(connection));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(FakeTransactionBehavior<,>));
        register(services);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    public static async Task<Result<string>> SendAsync(ServiceProvider provider, string value)
    {
        using IServiceScope scope = provider.CreateScope();

        return await scope.ServiceProvider
            .GetRequiredService<ISender>()
            .SendAsync<FakeWriteCommand, Result<string>>(
                new FakeWriteCommand(value),
                CancellationToken.None);
    }

    public static async Task<int> CountRowsAsync(SqliteConnection connection)
    {
        await using FakeDbContext context = new(OptionsFor(connection));

        return await context.Rows.CountAsync();
    }

    private static DbContextOptions<FakeDbContext> OptionsFor(SqliteConnection connection) =>
        new DbContextOptionsBuilder<FakeDbContext>().UseSqlite(connection).Options;
}
