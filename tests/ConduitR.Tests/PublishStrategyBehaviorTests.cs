using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ConduitR;
using ConduitR.Abstractions;
using ConduitR.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class PublishStrategyBehaviorTests
{
    [Fact]
    public async Task PublishStrategy_Sequential_runs_all_handlers_for_three_or_more()
    {
        var services = new ServiceCollection();
        services.AddConduit(cfg => cfg.PublishStrategy = PublishStrategy.Sequential);

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
        var note = new Note3(new Counter());

        var exception = await Assert.ThrowsAsync<AggregateException>(() => mediator.Publish(note));

        Assert.Single(exception.InnerExceptions);
        Assert.IsType<InvalidOperationException>(exception.InnerExceptions[0]);
        Assert.Equal(2, note.Counter.Count);
    }

    [Fact]
    public async Task PublishStrategy_Parallel_invokes_all_non_throwing_handlers()
    {
        var services = new ServiceCollection();
        services.AddConduit(cfg => cfg.PublishStrategy = PublishStrategy.Parallel);

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
        var note = new Note2(new Counter());

        await mediator.Publish(note);

        Assert.Equal(2, note.Counter.Count);
    }

    public sealed record Note2(Counter Counter) : INotification;
    public sealed class FirstIncrements2 : INotificationHandler<Note2> { public Task Handle(Note2 notification, CancellationToken ct) { notification.Counter.Increment(); return Task.CompletedTask; } }
    public sealed class SecondIncrements2 : INotificationHandler<Note2> { public Task Handle(Note2 notification, CancellationToken ct) { notification.Counter.Increment(); return Task.CompletedTask; } }

    public sealed record Note3(Counter Counter) : INotification;
    public sealed class FirstThrows3 : INotificationHandler<Note3> { public Task Handle(Note3 notification, CancellationToken ct) => throw new InvalidOperationException("boom"); }
    public sealed class SecondIncrements3 : INotificationHandler<Note3> { public Task Handle(Note3 notification, CancellationToken ct) { notification.Counter.Increment(); return Task.CompletedTask; } }
    public sealed class ThirdIncrements3 : INotificationHandler<Note3> { public Task Handle(Note3 notification, CancellationToken ct) { notification.Counter.Increment(); return Task.CompletedTask; } }

    public sealed class Counter { private int _i; public int Count => _i; public void Increment() => Interlocked.Increment(ref _i); }
}
