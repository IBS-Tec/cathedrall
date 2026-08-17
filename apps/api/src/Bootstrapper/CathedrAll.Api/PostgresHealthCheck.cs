using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace CathedrAll.Api;

internal sealed class PostgresHealthCheck(IConfiguration configuration) : IHealthCheck
{
    internal const string ConnectionName = "CathedrAll";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        string? connectionString = configuration.GetConnectionString(ConnectionName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy(
                $"Connection string '{ConnectionName}' is not configured.");
        }

        try
        {
            await using NpgsqlConnection connection = new(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using NpgsqlCommand command = new("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Postgres is unreachable.", exception);
        }
    }
}
