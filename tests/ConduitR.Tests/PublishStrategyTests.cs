using ConduitR;
using ConduitR.Abstractions;
using ConduitR.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class PublishStrategyTests
{
    [Fact]
    public async Task StopOnFirstException_stops_subsequent_handlers()
    {
        var services = new ServiceCollection();
        services.AddConduit(cfg => { cfg.PublishStrategy = PublishStrategy.StopOnFirstException; });
        services.AddTransient<INotificationHandler<Note>, FirstThrows>();
        services.AddTransient<INotificationHandler<Note>, SecondIncrements>();

        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var counter = new Counter();
        var note = new Note(counter);

        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Publish(note));

        Assert.Equal(0, counter.Count);
    }

    public sealed record Note(Counter C) : INotification;
    public sealed class FirstThrows : INotificationHandler<Note>
    {
        public Task Handle(Note n, CancellationToken ct) => throw new InvalidOperationException("boom");
    }
    public sealed class SecondIncrements : INotificationHandler<Note>
    {
        public Task Handle(Note n, CancellationToken ct) { n.C.Inc(); return Task.CompletedTask; }
    }
    public sealed class Counter { private int _i; public int Count => _i; public void Inc() => Interlocked.Increment(ref _i); }
}
