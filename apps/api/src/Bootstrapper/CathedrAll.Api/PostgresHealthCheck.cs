using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace CathedrAll.Api;

internal sealed class PostgresHealthCheck(IConfiguration configuracao) : IHealthCheck
{
    internal const string NomeDaConexao = "CathedrAll";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        string? conexao = configuracao.GetConnectionString(NomeDaConexao);

        if (string.IsNullOrWhiteSpace(conexao))
        {
            return HealthCheckResult.Unhealthy(
                $"Connection string '{NomeDaConexao}' não configurada.");
        }

        try
        {
            await using NpgsqlConnection ligacao = new(conexao);
            await ligacao.OpenAsync(cancellationToken);

            await using NpgsqlCommand comando = new("SELECT 1", ligacao);
            await comando.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception excecao)
        {
            return HealthCheckResult.Unhealthy("Postgres inacessível.", excecao);
        }
    }
}
