using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;

// bring extension methods into scope
using ConduitR.DependencyInjection;
using MediatR;

// alias overlapping namespaces to avoid ambiguity
using C = ConduitR.Abstractions;
using M = MediatR;

public class Program
{
    public static void Main(string[] args)
        => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}

#region Baseline SEND / PUBLISH / STREAM

[MemoryDiagnoser, MarkdownExporter, CsvExporter]
public class SendBenchmarks
{
    private C.IMediator _conduit = default!;
    private M.IMediator _mediatr = default!;
    private Ping _req = new("bench");

    [GlobalSetup]
    public void Setup()
    {
        // ConduitR
        var s1 = new ServiceCollection();
        s1.AddConduit(cfg => cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly()));
        _conduit = s1.BuildServiceProvider().GetRequiredService<C.IMediator>();

        // MediatR (v11 registration style)
        var s2 = new ServiceCollection();
        s2.AddMediatR(Assembly.GetExecutingAssembly());
        _mediatr = s2.BuildServiceProvider().GetRequiredService<M.IMediator>();
    }

    [Benchmark(Baseline = true, Description = "ConduitR.Send")]
    public Task ConduitR_Send() => _conduit.Send(_req).AsTask();

    [Benchmark(Description = "MediatR.Send")]
    public Task MediatR_Send() => _mediatr.Send(_req);

    public sealed record Ping(string Name) : C.IRequest<string>, M.IRequest<string>;
    public sealed class PingHandler :
        C.IRequestHandler<Ping, string>,
        M.IRequestHandler<Ping, string>
    {
        public ValueTask<string> Handle(Ping request, CancellationToken ct)
            => ValueTask.FromResult(request.Name);
        Task<string> M.IRequestHandler<Ping, string>.Handle(Ping request, CancellationToken ct)
            => Task.FromResult(request.Name);
    }
}

[MemoryDiagnoser, MarkdownExporter, CsvExporter]
public class PublishBenchmarks
{
    private C.IMediator _conduit = default!;
    private M.IMediator _mediatr = default!;
    private Note _n = new();

    [GlobalSetup]
    public void Setup()
    {
        var s1 = new ServiceCollection();
        s1.AddConduit(cfg => cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly()));
        _conduit = s1.BuildServiceProvider().GetRequiredService<C.IMediator>();

        var s2 = new ServiceCollection();
        s2.AddMediatR(Assembly.GetExecutingAssembly());
        _mediatr = s2.BuildServiceProvider().GetRequiredService<M.IMediator>();
    }

    [Benchmark(Baseline = true, Description = "ConduitR.Publish (2 handlers)")]
    public Task ConduitR_Publish() => _conduit.Publish(_n);

    [Benchmark(Description = "MediatR.Publish (2 handlers)")]
    public Task MediatR_Publish() => _mediatr.Publish(_n);

    public sealed record Note : C.INotification, M.INotification;

    public sealed class H1 :
        C.INotificationHandler<Note>,
        M.INotificationHandler<Note>
    {
        Task C.INotificationHandler<Note>.Handle(Note n, CancellationToken ct) => Task.CompletedTask;
        Task M.INotificationHandler<Note>.Handle(Note n, CancellationToken ct) => Task.CompletedTask;
    }

    public sealed class H2 :
        C.INotificationHandler<Note>,
        M.INotificationHandler<Note>
    {
        Task C.INotificationHandler<Note>.Handle(Note n, CancellationToken ct) => Task.CompletedTask;
        Task M.INotificationHandler<Note>.Handle(Note n, CancellationToken ct) => Task.CompletedTask;
    }
}

[MemoryDiagnoser, MarkdownExporter, CsvExporter]
public class StreamBenchmarks
{
    private ConduitR.Mediator _conduit = default!; // concrete exposes CreateStream
    private M.IMediator _mediatr = default!;
    private Range _r = new(0, 16);

    [GlobalSetup]
    public void Setup()
    {
        var s1 = new ServiceCollection();
        s1.AddConduit(cfg => cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly()));
        _conduit = (ConduitR.Mediator)s1.BuildServiceProvider().GetRequiredService<C.IMediator>();

        var s2 = new ServiceCollection();
        s2.AddMediatR(Assembly.GetExecutingAssembly());
        _mediatr = s2.BuildServiceProvider().GetRequiredService<M.IMediator>();
    }

    [Benchmark(Baseline = true, Description = "ConduitR.CreateStream consume 16")]
    public async Task ConduitR_Stream()
    {
        await foreach (var _ in _conduit.CreateStream(_r)) { }
    }

    [Benchmark(Description = "MediatR.CreateStream consume 16")]
    public async Task MediatR_Stream()
    {
        await foreach (var _ in _mediatr.CreateStream(_r)) { }
    }

    public sealed record Range(int Start, int Count) : C.IStreamRequest<int>, M.IStreamRequest<int>;

    public sealed class RangeHandler :
        C.IStreamRequestHandler<Range, int>,
        M.IStreamRequestHandler<Range, int>
    {
        async IAsyncEnumerable<int> C.IStreamRequestHandler<Range, int>.Handle(Range r, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            for (int i = 0; i < r.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return r.Start + i;
            }
        }

        IAsyncEnumerable<int> M.IStreamRequestHandler<Range, int>.Handle(Range r, CancellationToken ct)
            => ((C.IStreamRequestHandler<Range, int>)this).Handle(r, ct);
    }
}

#endregion

#region Extended scenarios

/// <summary>Send with N noop behaviors (0,1,2).</summary>
[MemoryDiagnoser, MarkdownExporter, CsvExporter]
public class SendWithBehaviorsBenchmarks
{
    [Params(0,1,2)]
    public int BehaviorCount;

    private C.IMediator _conduit = default!;
    private M.IMediator _mediatr = default!;
    private Ping _req = new("bench");

    [GlobalSetup]
    public void Setup()
    {
        // ConduitR
        var s1 = new ServiceCollection();
        s1.AddConduit(cfg => cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly()));
        for (int i = 0; i < BehaviorCount; i++)
            s1.AddTransient(typeof(C.IPipelineBehavior<,>), typeof(NoopBehavior<,>));
        _conduit = s1.BuildServiceProvider().GetRequiredService<C.IMediator>();

        // MediatR
        var s2 = new ServiceCollection();
        s2.AddMediatR(Assembly.GetExecutingAssembly());
        for (int i = 0; i < BehaviorCount; i++)
            s2.AddTransient(typeof(M.IPipelineBehavior<,>), typeof(NoopBehavior<,>));
        _mediatr = s2.BuildServiceProvider().GetRequiredService<M.IMediator>();
    }

    [Benchmark(Baseline=true, Description="ConduitR.Send + behaviors")]
    public Task ConduitR_Send() => _conduit.Send(_req).AsTask();

    [Benchmark(Description="MediatR.Send + behaviors")]
    public Task MediatR_Send() => _mediatr.Send(_req);

    public sealed record Ping(string Name) : C.IRequest<string>, M.IRequest<string>;

    public sealed class Handler :
        C.IRequestHandler<Ping, string>,
        M.IRequestHandler<Ping, string>
    {
        public ValueTask<string> Handle(Ping request, CancellationToken ct) => ValueTask.FromResult(request.Name);
        Task<string> M.IRequestHandler<Ping, string>.Handle(Ping request, CancellationToken ct) => Task.FromResult(request.Name);
    }
}

/// <summary>Noop behavior implementing both frameworks' interfaces.</summary>
public sealed class NoopBehavior<TReq, TRes> :
    C.IPipelineBehavior<TReq, TRes>,
    M.IPipelineBehavior<TReq, TRes>
    where TReq : C.IRequest<TRes>, M.IRequest<TRes> // 👈 satisfy both frameworks
{
    // ConduitR behavior
    public ValueTask<TRes> Handle(
        TReq request,
        CancellationToken ct,
        C.RequestHandlerDelegate<TRes> next)
        => next();

    // MediatR behavior (explicit impl to avoid ambiguity)
    Task<TRes> M.IPipelineBehavior<TReq, TRes>.Handle(
        TReq request,
        M.RequestHandlerDelegate<TRes> next,
        CancellationToken ct)
        => next();
}


/// <summary>Publish with varying number of handlers.</summary>
[MemoryDiagnoser, MarkdownExporter, CsvExporter]
public class PublishHandlerCountsBenchmarks
{
    [Params(2,5,10)]
    public int HandlerCount;

    private C.IMediator _conduit = default!;
    private M.IMediator _mediatr = default!;
    private Note _n = new();

    [GlobalSetup]
    public void Setup()
    {
        var s1 = new ServiceCollection();
        s1.AddConduit(cfg => cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly()));
        for (int i = 0; i < HandlerCount; i++)
            s1.AddTransient(typeof(C.INotificationHandler<Note>), typeof(NopNoteHandler));
        _conduit = s1.BuildServiceProvider().GetRequiredService<C.IMediator>();

        var s2 = new ServiceCollection();
        s2.AddMediatR(Assembly.GetExecutingAssembly());
        for (int i = 0; i < HandlerCount; i++)
            s2.AddTransient(typeof(M.INotificationHandler<Note>), typeof(NopNoteHandler));
        _mediatr = s2.BuildServiceProvider().GetRequiredService<M.IMediator>();
    }

    [Benchmark(Baseline=true, Description="ConduitR.Publish (N handlers)")]
    public Task ConduitR_Publish() => _conduit.Publish(_n);

    [Benchmark(Description="MediatR.Publish (N handlers)")]
    public Task MediatR_Publish() => _mediatr.Publish(_n);

    public sealed record Note : C.INotification, M.INotification;
    public sealed class NopNoteHandler :
        C.INotificationHandler<Note>,
        M.INotificationHandler<Note>
    {
        Task C.INotificationHandler<Note>.Handle(Note n, CancellationToken ct) => Task.CompletedTask;
        Task M.INotificationHandler<Note>.Handle(Note n, CancellationToken ct) => Task.CompletedTask;
    }
}

/// <summary>Stream with varying item counts.</summary>
[MemoryDiagnoser, MarkdownExporter, CsvExporter]
public class StreamCountsBenchmarks
{
    [Params(16, 256, 1024)]
    public int Count;

    private ConduitR.Mediator _conduit = default!;
    private M.IMediator _mediatr = default!;
    private Range _r = default!;

    [GlobalSetup]
    public void Setup()
    {
        _r = new Range(0, Count);

        var s1 = new ServiceCollection();
        s1.AddConduit(cfg => cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly()));
        _conduit = (ConduitR.Mediator)s1.BuildServiceProvider().GetRequiredService<C.IMediator>();

        var s2 = new ServiceCollection();
        s2.AddMediatR(Assembly.GetExecutingAssembly());
        _mediatr = s2.BuildServiceProvider().GetRequiredService<M.IMediator>();
    }

    [Benchmark(Baseline=true, Description="ConduitR.CreateStream consume N")]
    public async Task ConduitR_Stream()
    {
        await foreach (var _ in _conduit.CreateStream(_r)) { }
    }

    [Benchmark(Description="MediatR.CreateStream consume N")]
    public async Task MediatR_Stream()
    {
        await foreach (var _ in _mediatr.CreateStream(_r)) { }
    }

    public sealed record Range(int Start, int Count) : C.IStreamRequest<int>, M.IStreamRequest<int>;
    public sealed class Handler :
        C.IStreamRequestHandler<Range, int>,
        M.IStreamRequestHandler<Range, int>
    {
        async IAsyncEnumerable<int> C.IStreamRequestHandler<Range, int>.Handle(Range r, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            for (int i = 0; i < r.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return r.Start + i;
            }
        }
        IAsyncEnumerable<int> M.IStreamRequestHandler<Range, int>.Handle(Range r, CancellationToken ct)
            => ((C.IStreamRequestHandler<Range, int>)this).Handle(r, ct);
    }
}

#endregion
