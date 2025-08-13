using System.Reflection;
using ConduitR.Abstractions;
using ConduitR.DependencyInjection;
using ConduitR.Processing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class ProcessingTests
{
    private IMediator BuildMediator()
    {
        var services = new ServiceCollection();
        services.AddConduit(cfg => cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly()));
        services.AddConduitProcessing(Assembly.GetExecutingAssembly());
        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Pre_and_Post_processors_are_invoked()
    {
        var mediator = BuildMediator();
        var ctx = new Ctx();
        var res = await mediator.Send(new ProcRequest(ctx));

        Assert.True(ctx.PreRan);
        Assert.True(ctx.PostRan);
        Assert.Equal("ok", res);
    }

    public sealed class Ctx { public bool PreRan; public bool PostRan; }

    public sealed record ProcRequest(Ctx Ctx) : IRequest<string>;
    public sealed class ProcHandler : IRequestHandler<ProcRequest, string>
    {
        public ValueTask<string> Handle(ProcRequest request, CancellationToken cancellationToken) => ValueTask.FromResult("ok");
    }

    public sealed class MyPre : IRequestPreProcessor<ProcRequest>
    {
        public Task Process(ProcRequest request, CancellationToken cancellationToken) { request.Ctx.PreRan = true; return Task.CompletedTask; }
    }
    public sealed class MyPost : IRequestPostProcessor<ProcRequest, string>
    {
        public Task Process(ProcRequest request, string response, CancellationToken cancellationToken) { request.Ctx.PostRan = true; return Task.CompletedTask; }
    }
}
