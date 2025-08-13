using System.Reflection;
using ConduitR.Abstractions;
using ConduitR.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class BasicTests
{
    private IMediator BuildMediator()
    {
        var services = new ServiceCollection();
        services.AddConduit(cfg => cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly()));
        services.AddConduit(cfg => cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly()));
        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Send_Returns_Response()
    {
        var mediator = BuildMediator();
        var res = await mediator.Send(new Ping("x"));
        Assert.Equal("Hello, x", res);
    }

    [Fact]
    public async Task Publish_Invokes_All_Handlers()
    {
        var mediator = BuildMediator();
        var n = new Counter();
        await mediator.Publish(new Note(n));

        Assert.Equal(2, n.Count);
    }

    // Test request/handler
    public sealed record Ping(string Name) : IRequest<string>;
    public sealed class PingHandler : IRequestHandler<Ping, string>
    {
        public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
            => ValueTask.FromResult($"Hello, {request.Name}");
    }

    // Test notification/handlers
    public sealed record Note(Counter C) : INotification;
    public sealed class LogA : INotificationHandler<Note>
    {
        public Task Handle(Note notification, CancellationToken cancellationToken) { notification.C.Increment(); return Task.CompletedTask; }
    }
    public sealed class LogB : INotificationHandler<Note>
    {
        public Task Handle(Note notification, CancellationToken cancellationToken) { notification.C.Increment(); return Task.CompletedTask; }
    }

    public sealed class Counter
    {
        private int _i;
        public int Count => _i;
        public void Increment() => Interlocked.Increment(ref _i);
    }
}
