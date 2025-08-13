using System.Reflection;
using ConduitR.Abstractions;
using ConduitR.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class StreamingTests
{
    private ConduitR.Mediator BuildMediator()
    {
        var services = new ServiceCollection();
        services.AddConduit(cfg => cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly()));
        return (ConduitR.Mediator)services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Stream_returns_sequence()
    {
        var mediator = BuildMediator();
        var results = new List<int>();
        await foreach (var x in mediator.CreateStream(new RangeStream(5, 3)))
        {
            results.Add(x);
        }
        Assert.Equal(new[] {5,6,7}, results);
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
}
