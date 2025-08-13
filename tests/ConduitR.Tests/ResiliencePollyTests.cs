using System.Reflection;
using ConduitR.Abstractions;
using ConduitR.DependencyInjection;
using ConduitR.Resilience.Polly;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class ResiliencePollyTests
{
    [Fact]
    public async Task Retry_retries_until_success()
    {
        var services = new ServiceCollection();
        services.AddConduit(cfg => cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly()));
        services.AddConduitResiliencePolly(o => { o.RetryCount = 2; o.Timeout = TimeSpan.FromSeconds(1); });
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var res = await mediator.Send(new FlakyRequest(2)); // fails twice, then succeeds
        Assert.Equal("ok", res);
    }

    [Fact]
    public async Task Timeout_triggers_on_slow_handler()
    {
        var services = new ServiceCollection();
        services.AddConduit(cfg => cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly()));
        services.AddConduitResiliencePolly(o => { o.Timeout = TimeSpan.FromMilliseconds(50); o.RetryCount = 0; });
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<Polly.Timeout.TimeoutRejectedException>(() => mediator.Send(new SlowRequest(200)).AsTask());
    }

    public sealed record FlakyRequest(int FailuresBeforeSuccess) : IRequest<string>;
    public sealed class FlakyHandler : IRequestHandler<FlakyRequest, string>
    {
        private static int _calls;
        public async ValueTask<string> Handle(FlakyRequest request, CancellationToken ct)
        {
            var call = Interlocked.Increment(ref _calls);
            if (call <= request.FailuresBeforeSuccess) throw new InvalidOperationException("boom");
            await Task.Yield();
            _calls = 0; // reset for other tests
            return "ok";
        }
    }

    public sealed record SlowRequest(int DelayMs) : IRequest<string>;
    public sealed class SlowHandler : IRequestHandler<SlowRequest, string>
    {
        public async ValueTask<string> Handle(SlowRequest request, CancellationToken ct)
        {
            await Task.Delay(request.DelayMs, ct);
            return "slow";
        }
    }
}
