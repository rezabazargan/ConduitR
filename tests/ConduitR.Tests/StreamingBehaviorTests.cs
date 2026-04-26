using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ConduitR;
using ConduitR.Abstractions;
using ConduitR.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class StreamingBehaviorTests
{
    [Fact]
    public async Task CreateStream_invokes_registered_stream_pipeline_behavior()
    {
        var services = new ServiceCollection();
        services.AddConduit(cfg => cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly()));
        services.AddTransient<IStreamPipelineBehavior<RangeStream, int>, RangeCountingBehavior>();

        var mediator = (Mediator)services.BuildServiceProvider().GetRequiredService<IMediator>();
        var results = new List<int>();

        await foreach (var x in mediator.CreateStream(new RangeStream(1, 3)))
        {
            results.Add(x);
        }

        Assert.Equal(new[] { 1, 2, 3 }, results);
        Assert.Equal(1, RangeCountingBehavior.Invocations);
    }

    public sealed record RangeStream(int Start, int Count) : IStreamRequest<int>;
    public sealed class RangeHandler : IStreamRequestHandler<RangeStream, int>
    {
        public async IAsyncEnumerable<int> Handle(RangeStream request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            for (var i = 0; i < request.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return request.Start + i;
            }
        }
    }

    public sealed class RangeCountingBehavior : IStreamPipelineBehavior<RangeStream, int>
    {
        public static int Invocations;

        public IAsyncEnumerable<int> Handle(RangeStream request, CancellationToken cancellationToken, StreamHandlerDelegate<int> next)
        {
            Interlocked.Increment(ref Invocations);
            return next();
        }
    }
}
