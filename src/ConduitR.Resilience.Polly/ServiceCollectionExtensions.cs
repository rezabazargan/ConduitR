using ConduitR.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ConduitR.Resilience.Polly;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConduitResiliencePolly(this IServiceCollection services, Action<ConduitResilienceOptions>? configure = null)
    {
        if (configure is null) services.AddOptions<ConduitResilienceOptions>();
        else services.AddOptions<ConduitResilienceOptions>().Configure(configure);

        services.TryAddSingleton(typeof(IConduitPolicyProvider<>), typeof(DefaultPolicyProvider<>));
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(ResilienceBehavior<,>)));
        return services;
    }
}
