
# ConduitR

Lightweight, fast, and familiar mediator for .NET — designed to feel natural for MediatR users, with a focus on performance, simplicity, and great DX.

[![Build](https://github.com/rezabazargan/ConduitR/actions/workflows/build.yml/badge.svg)](https://github.com/rezabazargan/ConduitR/actions)
[![NuGet](https://img.shields.io/nuget/v/ConduitR.svg)](https://www.nuget.org/packages/ConduitR)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Why ConduitR?

- **Familiar**: same mental model as MediatR (`IRequest<T>`, `IRequestHandler`, notifications, behaviors).
- **Fast**: hot path optimized (cached pipelines per request type, low allocations, ValueTask).
- **Modular**: add-on packages for Validation, AspNetCore helpers, Processing, and Resilience (Polly).
- **Observable**: built-in `ActivitySource` for OpenTelemetry (`Send`, `Publish`, `Stream`).

## Packages

| Package | What it adds |
|---|---|
| `ConduitR` | Core mediator + telemetry |
| `ConduitR.Abstractions` | Public contracts (requests, handlers, behaviors, etc.) |
| `ConduitR.DependencyInjection` | `AddConduit(...)` + assembly scanning |
| `ConduitR.Validation.FluentValidation` | Validation behavior + DI helpers (optional) |
| `ConduitR.AspNetCore` | ProblemDetails middleware + Minimal API helpers (optional) |
| `ConduitR.Processing` | Pre/Post processors as behaviors (optional) |
| `ConduitR.Resilience.Polly` | Retry / Timeout / CircuitBreaker behaviors (optional) |

## Install

```bash
dotnet add package ConduitR
dotnet add package ConduitR.Abstractions
dotnet add package ConduitR.DependencyInjection

# Optional add-ons
dotnet add package ConduitR.Validation.FluentValidation
dotnet add package ConduitR.AspNetCore
dotnet add package ConduitR.Processing
dotnet add package ConduitR.Resilience.Polly
````

## Quick start

```csharp
// Program.cs
using System.Reflection;
using ConduitR;
using ConduitR.Abstractions;
using ConduitR.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConduit(cfg =>
{
    cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly());
    cfg.PublishStrategy = PublishStrategy.Parallel; // Parallel (default), Sequential, StopOnFirstException
});

var app = builder.Build();
app.Run();

// A request + handler
public sealed record Ping(string Name) : IRequest<string>;

public sealed class PingHandler : IRequestHandler<Ping, string>
{
    public ValueTask<string> Handle(Ping request, CancellationToken ct)
        => ValueTask.FromResult($"Hello, {request.Name}!");
}
```

Use the mediator anywhere (DI):

```csharp
var result = await mediator.Send(new Ping("ConduitR"));
```

## Notifications (Publish)

```csharp
public sealed record UserRegistered(string Email) : INotification;

public sealed class SendWelcomeEmail : INotificationHandler<UserRegistered> { /* ... */ }
public sealed class AuditLog : INotificationHandler<UserRegistered> { /* ... */ }

// Strategy via AddConduit(...):
// Parallel (default), Sequential (aggregate errors), StopOnFirstException (short-circuit)
await mediator.Publish(new UserRegistered("x@y.com"));
```

## Streaming

```csharp
public sealed record Ticks(int Count) : IStreamRequest<string>;

public sealed class TicksHandler : IStreamRequestHandler<Ticks, string>
{
    public async IAsyncEnumerable<string> Handle(Ticks req, [EnumeratorCancellation] CancellationToken ct)
    {
        for (var i = 1; i <= req.Count; i++) { ct.ThrowIfCancellationRequested(); await Task.Delay(100, ct); yield return $"tick-{i}"; }
    }
}

// Consume
await foreach (var s in mediator.CreateStream(new Ticks(3))) Console.WriteLine(s);
```

## Validation (FluentValidation)

```csharp
using ConduitR.Validation.FluentValidation;

builder.Services.AddConduitValidation(typeof(Program).Assembly);

public sealed record CreateOrder(string? Sku, int Qty) : IRequest<string>;
public sealed class CreateOrderValidator : AbstractValidator<CreateOrder>
{
    public CreateOrderValidator() { RuleFor(x => x.Sku).NotEmpty(); RuleFor(x => x.Qty).GreaterThan(0); }
}
```

## AspNetCore helpers

```csharp
using ConduitR.AspNetCore;

// ProblemDetails (400s for validation, 5xx handled)
builder.Services.AddConduitProblemDetails();

var app = builder.Build();
app.UseConduitProblemDetails();

// Minimal API mapper
app.MapMediatorPost<CreateOrder, string>("/orders");
```

## Pre/Post processors

```csharp
using ConduitR.Processing;

builder.Services.AddConduitProcessing(typeof(Program).Assembly);

public sealed class AuditPre : IRequestPreProcessor<CreateOrder>
{
    public Task Process(CreateOrder req, CancellationToken ct) { /* audit */ return Task.CompletedTask; }
}

public sealed class MetricsPost : IRequestPostProcessor<CreateOrder, string>
{
    public Task Process(CreateOrder req, string res, CancellationToken ct) { /* metrics */ return Task.CompletedTask; }
}
```

## Resilience (Polly)

```csharp
using ConduitR.Resilience.Polly;

builder.Services.AddConduitResiliencePolly(o =>
{
    o.RetryCount = 3;                    // exponential backoff
    o.Timeout = TimeSpan.FromSeconds(1); // per-attempt timeout (pessimistic)
    o.CircuitBreakerEnabled = true;
    o.CircuitBreakerFailures = 5;
    o.CircuitBreakerDuration = TimeSpan.FromSeconds(30);
});
```

## Telemetry (OpenTelemetry)

```csharp
using ConduitR;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("YourApp"))
    .WithTracing(b => b
        .AddSource(ConduitRTelemetry.ActivitySourceName) // "ConduitR"
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());
```

Spans: `Mediator.Send`, `Mediator.Publish`, `Mediator.Stream` (+ tags & exception events).

## Performance

* Cached Send/Stream pipelines per `(TRequest,TResponse)` (no per-call reflection/delegate build).
* Lean publish path, minimal allocations.
* `ValueTask` in core, one-type-per-file organization.

See `docs/perf-pipeline-cache.md` for details.

## Migrate from MediatR

| MediatR                                    | ConduitR                                 |
| ------------------------------------------ | ---------------------------------------- |
| `IMediator.Send`, `.Publish`               | same                                     |
| `IRequest<TResponse>`                      | same                                     |
| `IRequestHandler<TReq,TRes>`               | same                                     |
| `INotification`, `INotificationHandler<T>` | same                                     |
| Pipeline behaviors                         | same (`IPipelineBehavior<TReq,TRes>`)    |
| Pre/Post processors                        | via **ConduitR.Processing** (behaviors)  |
| Validation (FluentValidation)              | **ConduitR.Validation.FluentValidation** |
| ASP.NET Core helpers                       | **ConduitR.AspNetCore**                  |
| Resilience                                 | **ConduitR.Resilience.Polly**            |

## Samples

* `samples/Samples.WebApi` — minimal API, ProblemDetails, validation, and streaming endpoint.

## Versioning & Releases

* Semantic versioning. Latest stable: **1.0.2**
* See GitHub Releases for notes.

## Contributing

PRs welcome!
Dev loop:

```bash
dotnet restore
dotnet build
dotnet test
```

(Optional) microbenchmarks:

```bash
dotnet run -c Release --project benchmarks/ConduitR.Benchmarks/ConduitR.Benchmarks.csproj
```

## License

MIT — see [LICENSE](LICENSE).



### Commit it
```bash
git add README.md
git commit -m "docs: refresh README with streaming, publish strategies, processing, resilience, telemetry, perf"
git push
````

