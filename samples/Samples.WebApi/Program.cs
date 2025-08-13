using System.Reflection;
using ConduitR.Abstractions;
using ConduitR.DependencyInjection;
using ConduitR.Validation.FluentValidation;
using ConduitR.AspNetCore;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ConduitR.Processing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConduit(cfg =>
{
    cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly());
    cfg.AddBehavior(typeof(LoggingBehavior<,>));
});

builder.Services.AddConduitValidation(Assembly.GetExecutingAssembly());
builder.Services.AddConduitProblemDetails();
builder.Services.AddConduitProcessing(typeof(Program).Assembly);
var app = builder.Build();

app.UseConduitProblemDetails();

app.MapGet("/time/{tz}", async (string tz, IMediator mediator, CancellationToken ct)
    => await mediator.Send(new GetTimeQuery(tz), ct));

// Streaming endpoint: yields 'count' ticks
app.MapGet("/ticks/{count:int}", async (int count, ConduitR.Mediator mediator, HttpResponse resp, CancellationToken ct) =>
{
    resp.ContentType = "application/x-ndjson";

    await foreach (var item in mediator.CreateStream(new TicksQuery(count), ct).WithCancellation(ct))
    {
        await resp.WriteAsync(item + "\n", ct);
        await resp.Body.FlushAsync(ct);
    }

    return Results.Empty; // 200 OK with streamed body
});

app.MapMediatorPost<Echo, string>("/echo");

app.Run();

// Requests/Handlers
public sealed record GetTimeQuery(string TimeZoneId) : IRequest<string>;
public sealed class GetTimeHandler : IRequestHandler<GetTimeQuery, string>
{
    public ValueTask<string> Handle(GetTimeQuery request, CancellationToken ct)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId);
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
        return ValueTask.FromResult(now.ToString("O"));
    }
}

public sealed class GetTimeQueryValidator : AbstractValidator<GetTimeQuery>
{
    public GetTimeQueryValidator() => RuleFor(x => x.TimeZoneId).NotEmpty();
}

public sealed record Echo(string Value) : IRequest<string>;
public sealed class EchoHandler : IRequestHandler<Echo, string>
{
    public ValueTask<string> Handle(Echo request, CancellationToken ct) => ValueTask.FromResult(request.Value);
}
public sealed class EchoValidator : AbstractValidator<Echo>
{
    public EchoValidator() => RuleFor(x => x.Value).NotEmpty();
}

// Streaming sample
public sealed record TicksQuery(int Count) : IStreamRequest<string>;

public sealed class TicksHandler : IStreamRequestHandler<TicksQuery, string>
{
    public async IAsyncEnumerable<string> Handle(TicksQuery request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        for (var i = 1; i <= request.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(100, ct);
            yield return $"tick-{i}";
        }
    }
}

// Sample logging behavior
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        _logger.LogInformation("Handling {RequestType}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Handled {RequestType}", typeof(TRequest).Name);
        return response;
    }
}

public sealed class AuditPre : IRequestPreProcessor<GetTimeQuery>
{
    public Task Process(GetTimeQuery req, CancellationToken ct) { /* audit */ return Task.CompletedTask; }
}
public sealed class MetricsPost : IRequestPostProcessor<GetTimeQuery,string>
{
    public Task Process(GetTimeQuery req, string res, CancellationToken ct) { /* metrics */ return Task.CompletedTask; }
}
