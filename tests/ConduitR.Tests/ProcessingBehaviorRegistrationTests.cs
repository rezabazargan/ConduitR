using System.Reflection;
using ConduitR;
using ConduitR.Abstractions;
using ConduitR.DependencyInjection;
using ConduitR.Processing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class ProcessingBehaviorRegistrationTests
{
    [Fact]
    public void AddConduitProcessing_registers_pre_and_post_behaviors_without_assemblies()
    {
        var services = new ServiceCollection();
        services.AddConduit(cfg => cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly()));
        services.AddConduitProcessing();

        var provider = services.BuildServiceProvider();
        var behaviors = provider.GetServices<IPipelineBehavior<EchoRequest, string>>();

        Assert.Collection(behaviors,
            item => Assert.IsType<PreProcessingBehavior<EchoRequest, string>>(item),
            item => Assert.IsType<PostProcessingBehavior<EchoRequest, string>>(item));
    }

    public sealed record EchoRequest(string Message) : IRequest<string>;
    public sealed class EchoRequestHandler : IRequestHandler<EchoRequest, string>
    {
        public ValueTask<string> Handle(EchoRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(request.Message);
    }
}
