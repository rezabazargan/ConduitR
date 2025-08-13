# Telemetry & OpenTelemetry

ConduitR emits `System.Diagnostics.Activity` spans via a named `ActivitySource`:

- Source name: **ConduitR**
- Spans:
  - `Mediator.Send` (tags: `conduitr.request_type`, `conduitr.response_type`, `conduitr.behaviors.count`, `conduitr.elapsed_ms`)
  - `Mediator.Publish` (tags: `conduitr.notification_type`, `conduitr.handlers.count`, events per-handler with elapsed/error)

## Wire up with OpenTelemetry

```csharp
using ConduitR;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

builder.Services.AddOpenTelemetry()
    .ConfigureResource(rb => rb.AddService("YourApp"))
    .WithTracing(b => b
        .AddSource(ConduitRTelemetry.ActivitySourceName) // <- pick up ConduitR spans
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter()); // or ConsoleExporter for local testing
```

Now your ConduitR `Send`/`Publish` operations will appear in your traces.
