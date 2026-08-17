using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CathedrAll.Api.Tests;

public sealed class GlobalExceptionHandlerTests
{
    private const string Secret = "senha-do-banco-no-stack-trace";

    [Fact]
    public async Task Deve_responder_500_em_problem_json()
    {
        DefaultHttpContext context = NewContext();

        bool handled = await Handle(context);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
    }

    [Fact]
    public async Task Deve_levar_o_code_do_erro_inesperado()
    {
        DefaultHttpContext context = NewContext();

        await Handle(context);

        Assert.Contains(
            "\"code\":\"Server.UnexpectedFailure\"",
            await ReadBody(context),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Nao_deve_vazar_a_mensagem_da_excecao_no_corpo()
    {
        DefaultHttpContext context = NewContext();

        await Handle(context);

        Assert.DoesNotContain(Secret, await ReadBody(context), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cliente_que_desiste_nao_deve_escrever_corpo()
    {
        DefaultHttpContext context = NewContext();
        context.RequestAborted = new CancellationToken(canceled: true);

        bool handled = await Handle(context, new OperationCanceledException());

        Assert.True(handled);
        Assert.Empty(await ReadBody(context));
        Assert.NotEqual(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task Resposta_ja_iniciada_nao_deve_ser_tratada()
    {
        FeatureCollection features = new();
        features.Set<IHttpRequestFeature>(new HttpRequestFeature());
        features.Set<IHttpResponseFeature>(new StartedResponseFeature());

        DefaultHttpContext context = new(features) { RequestServices = NewProvider() };

        bool handled = await Handle(context);

        Assert.False(handled);
    }

    private static ServiceProvider NewProvider()
    {
        ServiceCollection services = new();

        services.AddProblemDetails();
        services.AddLogging();

        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext NewContext()
    {
        DefaultHttpContext context = new() { RequestServices = NewProvider() };
        context.Response.Body = new MemoryStream();

        return context;
    }

    private static async Task<bool> Handle(
        DefaultHttpContext context,
        Exception? exception = null)
    {
        GlobalExceptionHandler handler = new(NullLoggerFactory.Instance);

        return await handler.TryHandleAsync(
            context,
            exception ?? new InvalidOperationException(Secret),
            TestContext.Current.CancellationToken);
    }

    private static async Task<string> ReadBody(DefaultHttpContext context)
    {
        if (context.Response.Body is not MemoryStream body)
        {
            return string.Empty;
        }

        body.Position = 0;

        using StreamReader reader = new(body, Encoding.UTF8, leaveOpen: true);

        return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    }
}
