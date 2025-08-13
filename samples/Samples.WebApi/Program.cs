using System.Reflection;
using ConduitR.Abstractions;
using ConduitR.DependencyInjection;
using ConduitR.Validation.FluentValidation;
using ConduitR.AspNetCore;
using FluentValidation;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConduit(cfg =>
{
    cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly());
    cfg.AddBehavior(typeof(LoggingBehavior<,>));
});

builder.Services.AddConduitValidation(Assembly.GetExecutingAssembly());
builder.Services.AddConduitProblemDetails();

var app = builder.Build();

app.UseConduitProblemDetails();

app.MapGet("/time/{tz}", async (string tz, IMediator mediator, CancellationToken ct)
    => await mediator.Send(new GetTimeQuery(tz), ct));

// Demonstrate POST with automatic mediator send + validation
app.MapMediatorPost<Echo, string>("/echo");

app.Run();

// Sample request/handler
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

// Echo sample
public sealed record Echo(string Value) : IRequest<string>;
public sealed class EchoHandler : IRequestHandler<Echo, string>
{
    public ValueTask<string> Handle(Echo request, CancellationToken ct) => ValueTask.FromResult(request.Value);
}
public sealed class EchoValidator : AbstractValidator<Echo>
{
    public EchoValidator() => RuleFor(x => x.Value).NotEmpty();
}

// Sample logging behavior in the sample project (so the core stays dependency-free)
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
