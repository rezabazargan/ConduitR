using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ConduitR.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

public class AspNetCoreMiddlewareTests
{
    [Fact]
    public async Task Invoke_catches_exception_and_writes_problem_details()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/test";
        context.Response.Body = new MemoryStream();

        var middleware = new ConduitProblemDetailsMiddleware(
            next: _ => throw new InvalidOperationException("boom"),
            logger: NullLogger<ConduitProblemDetailsMiddleware>.Instance,
            options: Options.Create(new ConduitProblemDetailsOptions()));

        await middleware.Invoke(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var document = JsonDocument.Parse(context.Response.Body);

        Assert.Equal(500, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType);
        Assert.Equal("An error occurred", document.RootElement.GetProperty("title").GetString());
        Assert.Equal("boom", document.RootElement.GetProperty("detail").GetString());
        Assert.Equal("/test", document.RootElement.GetProperty("instance").GetString());
    }

    [Fact]
    public async Task Invoke_maps_argument_exception_to_bad_request_and_invokes_enrich()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/arg";
        context.Response.Body = new MemoryStream();

        var options = new ConduitProblemDetailsOptions
        {
            Enrich = (_, _, problem) => problem.Extensions["custom"] = "enriched"
        };

        var middleware = new ConduitProblemDetailsMiddleware(
            next: _ => throw new ArgumentException("invalid"),
            logger: NullLogger<ConduitProblemDetailsMiddleware>.Instance,
            options: Options.Create(options));

        await middleware.Invoke(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var document = JsonDocument.Parse(context.Response.Body);

        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal("Bad request", document.RootElement.GetProperty("title").GetString());
        Assert.Equal("invalid", document.RootElement.GetProperty("detail").GetString());
        Assert.Equal("enriched", document.RootElement.GetProperty("custom").GetString());
    }
}
