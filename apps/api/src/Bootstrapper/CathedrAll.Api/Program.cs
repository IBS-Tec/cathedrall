using CathedrAll.Api;
using CathedrAll.Kernel.Application;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddKernelApplication();
builder.Services.AddLoggingBehavior();

builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>(
        "postgres",
        tags: ["ready"],
        timeout: TimeSpan.FromSeconds(3));

WebApplication app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

await app.RunAsync();
