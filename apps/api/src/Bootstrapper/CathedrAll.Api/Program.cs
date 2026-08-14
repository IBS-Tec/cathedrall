WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

// Anônimo e sem detalhes: responde "Healthy" com 200, ou "Unhealthy" com 503.
// Quem consome é o monitoramento externo, que só precisa saber se sobe.
app.MapHealthChecks("/health");

await app.RunAsync();
