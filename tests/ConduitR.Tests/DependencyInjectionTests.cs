using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ConduitR;
using ConduitR.Abstractions;
using ConduitR.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class DependencyInjectionTests
{
    [Fact]
    public async Task AddConduit_registers_mediator_and_handlers_from_calling_assembly()
    {
        var services = new ServiceCollection();
        services.AddConduit();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new EchoRequest("hello"));

        Assert.Equal("hello", response);
    }

    [Fact]
    public async Task AddConduit_AddBehavior_applies_custom_pipeline_behavior()
    {
        CountingBehavior<EchoRequest, string>.HandleCount = 0;

        var services = new ServiceCollection();
        services.AddConduit(cfg => cfg
            .AddHandlersFromAssemblies(Assembly.GetExecutingAssembly())
            .AddBehavior(typeof(CountingBehavior<,>)));

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new EchoRequest("ok"));

        Assert.Equal("ok", response);
        Assert.Equal(1, CountingBehavior<EchoRequest, string>.HandleCount);
    }

    [Fact]
    public void AddBehavior_requires_open_generic_behavior_type()
    {
        var options = new ConduitOptions();

        Assert.Throws<ArgumentException>(() => options.AddBehavior(typeof(CountingBehavior<EchoRequest, string>)));
    }

    public sealed record EchoRequest(string Text) : IRequest<string>;

    public sealed class EchoHandler : IRequestHandler<EchoRequest, string>
    {
        public ValueTask<string> Handle(EchoRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(request.Text);
    }

    public sealed class CountingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public static int HandleCount;

        public async ValueTask<TResponse> Handle(TRequest request, CancellationToken ct, RequestHandlerDelegate<TResponse> next)
        {
            Interlocked.Increment(ref HandleCount);
            return await next().ConfigureAwait(false);
        }
    }
}
