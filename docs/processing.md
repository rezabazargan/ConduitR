# Pre/Post Processing

Add lightweight hooks that run before and after your handler without writing full behaviors.

```csharp
using ConduitR.Processing;

// registration
services.AddConduitProcessing(typeof(Program).Assembly);

// implement
public sealed class AuditPre : IRequestPreProcessor<CreateOrder>
{
    public Task Process(CreateOrder request, CancellationToken ct) { /* audit */ return Task.CompletedTask; }
}

public sealed class MetricsPost : IRequestPostProcessor<CreateOrder, CreateOrderResult>
{
    public Task Process(CreateOrder request, CreateOrderResult response, CancellationToken ct) { /* metrics */ return Task.CompletedTask; }
}
```
