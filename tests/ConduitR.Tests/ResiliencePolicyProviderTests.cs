using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ConduitR;
using ConduitR.Abstractions;
using ConduitR.DependencyInjection;
using ConduitR.Resilience.Polly;
using Microsoft.Extensions.DependencyInjection;
using Polly.CircuitBreaker;
using Xunit;

public class ResiliencePolicyWorkflowTests
{
    [Fact]
    public async Task AddConduitResiliencePolly_trips_circuit_breaker_after_failure()
    {
        var services = new ServiceCollection();
        services.AddConduit(cfg => cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly()));
        services.AddConduitResiliencePolly(o =>
        {
            o.CircuitBreakerEnabled = true;
            o.CircuitBreakerFailures = 1;
            o.RetryCount = 0;
            o.Timeout = null;
        });

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new CircuitBreakerRequest()).AsTask());
        await Assert.ThrowsAsync<BrokenCircuitException>(() => mediator.Send(new CircuitBreakerRequest()).AsTask());
    }

    [Fact]
    public async Task AddConduitResiliencePolly_retries_failed_handler()
    {
        FlakyHandler.Reset();

        var services = new ServiceCollection();
        services.AddConduit(cfg => cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly()));
        services.AddConduitResiliencePolly(o =>
        {
            o.RetryCount = 2;
            o.RetryBackoff = attempt => TimeSpan.FromMilliseconds(1);
            o.Timeout = null;
        });

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
        var result = await mediator.Send(new FlakyRequest());

        Assert.Equal("ok", result);
        Assert.Equal(3, FlakyHandler.Attempts);
    }

    public sealed record CircuitBreakerRequest() : IRequest<string>;
    public sealed record FlakyRequest() : IRequest<string>;

    public sealed class CircuitBreakerHandler : IRequestHandler<CircuitBreakerRequest, string>
    {
        private static int _calls;

        public ValueTask<string> Handle(CircuitBreakerRequest request, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _calls) == 1)
                throw new InvalidOperationException("boom");

            return ValueTask.FromResult("ok");
        }
    }

    public sealed class FlakyHandler : IRequestHandler<FlakyRequest, string>
    {
        public static int Attempts;

        public static void Reset() => Attempts = 0;

        public ValueTask<string> Handle(FlakyRequest request, CancellationToken cancellationToken)
        {
            var attempt = Interlocked.Increment(ref Attempts);
            if (attempt <= 2)
                throw new InvalidOperationException("boom");

            return ValueTask.FromResult("ok");
        }
    }
}
