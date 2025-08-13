# Resilience (Polly)

Enable retry/timeout/circuit-breaker around handlers with one line:

```csharp
using ConduitR.Resilience.Polly;

builder.Services.AddConduitResiliencePolly(o =>
{
    o.RetryCount = 3;                    // retry 3 times with exponential backoff
    o.Timeout = TimeSpan.FromSeconds(1); // per-attempt timeout
    o.CircuitBreakerEnabled = true;      // break after consecutive failures
    o.CircuitBreakerFailures = 5;
    o.CircuitBreakerDuration = TimeSpan.FromSeconds(30);
});
```

### Sample handler (flaky)
```csharp
public sealed record Ping(string Id) : IRequest<string>;
public sealed class PingHandler : IRequestHandler<Ping, string>
{
    private static int _calls;
    public ValueTask<string> Handle(Ping r, CancellationToken ct)
    {
        var call = Interlocked.Increment(ref _calls);
        if (call % 3 != 0) throw new Exception("transient");
        _calls = 0;
        return ValueTask.FromResult("pong:" + r.Id);
    }
}
```

Now `await mediator.Send(new Ping("1"))` will retry on exceptions, enforce a timeout per attempt, and trip the circuit breaker on sustained failures.
