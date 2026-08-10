using CathedrAll.Kernel.Behaviors;
using CathedrAll.Kernel.Messaging;
using CathedrAll.Kernel.Modules;
using CathedrAll.Pessoas;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Os módulos são listados explicitamente. Nada é descoberto por varredura de assembly:
// para saber o que a aplicação carrega, basta ler estas linhas (ADR-0012).
IModule[] modulos = [new PessoasModule()];

builder.Services.AddOpenApi();

builder.Services.AddMediator([.. modulos.Select(m => m.GetType().Assembly)]);

// Ordem importa: o primeiro registrado é o mais externo do pipeline.
builder.Services.AddPipelineBehavior(typeof(LoggingBehavior<,>));

// Atrás do Cloudflare Tunnel e do Traefik, a aplicação enxerga o IP do container.
// Sem isto o audit log exigido pela LGPD registra o IP errado — pior que não registrar,
// porque parece correto (ADR-0010).
//
// O padrão é NÃO confiar em nada: sem proxies configurados, os cabeçalhos são ignorados.
// Confiar em X-Forwarded-For sem restringir a origem torna o IP falsificável.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();

    foreach (var proxy in builder.Configuration.GetSection("ProxiesConfiaveis").Get<string[]>() ?? [])
    {
        options.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
    }
});

foreach (var modulo in modulos)
{
    modulo.Register(builder.Services, builder.Configuration);
}

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Consumido pelo monitoramento externo — que precisa ser externo, porque monitor
// rodando na própria máquina não avisa quando ela cai (ADR-0009).
app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("Health")
    .AllowAnonymous();

foreach (var modulo in modulos)
{
    modulo.MapEndpoints(app);
}

app.Run();
