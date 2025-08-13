using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ConduitR.Abstractions;
using ConduitR.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

BenchmarkRunner.Run<SendBenchmarks>();

[MemoryDiagnoser]
public class SendBenchmarks
{
    private IMediator _mediator = default!;
    private Ping _request = new("bench");

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddConduit(cfg => cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly()));
        var sp = services.BuildServiceProvider();
        _mediator = sp.GetRequiredService<IMediator>();
    }

    [Benchmark]
    public async Task Send_WithCache()
    {
        var _ = await _mediator.Send(_request);
    }

    public sealed record Ping(string Name) : IRequest<string>;
    public sealed class PingHandler : IRequestHandler<Ping, string>
    {
        public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken) => ValueTask.FromResult(request.Name);
    }
}
