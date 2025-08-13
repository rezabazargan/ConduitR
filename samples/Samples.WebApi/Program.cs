using System.Reflection;
using ConduitR.Abstractions;
using ConduitR.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConduit(cfg =>
{
    cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly());
    cfg.AddBehavior(typeof(LoggingBehavior<,>));
});

var app = builder.Build();

app.MapGet("/time/{tz}", async (string tz, IMediator mediator, CancellationToken ct)
    => await mediator.Send(new GetTimeQuery(tz), ct));

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
