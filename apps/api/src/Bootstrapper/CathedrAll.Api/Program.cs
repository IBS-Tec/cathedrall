using System.Diagnostics;
using System.Text.Json.Serialization;
using CathedrAll.Api;
using CathedrAll.Kernel.Application;
using CathedrAll.Pessoas;
using CathedrAll.Pessoas.Endpoints;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddKernelApplication();
builder.Services.AddLoggingBehavior();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDevelopmentCurrentUser(builder.Configuration);
}

builder.Services.AddPessoasDbContext(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString(PostgresHealthCheck.ConnectionName),
        npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "pessoas")));
builder.Services.AddPessoasTransactionBehavior();
builder.Services.AddPessoasHandlers();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);

builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>(
        "postgres",
        tags: ["ready"],
        timeout: TimeSpan.FromSeconds(3));

builder.Services.RequireCurrentUser();

WebApplication app = builder.Build();

app.UseExceptionHandler();

app.UseStatusCodePages();

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.MapPessoasEndpoints();

await app.RunAsync();
