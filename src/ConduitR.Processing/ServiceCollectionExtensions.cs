using System.Reflection;
using ConduitR.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ConduitR.Processing;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers pre/post processors discovered in the given assemblies and wires them via pipeline behaviors.
    /// </summary>
    public static IServiceCollection AddConduitProcessing(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Wire behaviors once
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(PreProcessingBehavior<,>)));
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(PostProcessingBehavior<,>)));

        if (assemblies is null || assemblies.Length == 0) return services;

        foreach (var asm in assemblies.Distinct())
        {
            foreach (var type in asm.GetTypes().Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition))
            {
                foreach (var iface in type.GetInterfaces())
                {
                    if (!iface.IsGenericType) continue;
                    var def = iface.GetGenericTypeDefinition();
                    if (def == typeof(IRequestPreProcessor<>)
                        || def == typeof(IRequestPostProcessor<,>))
                    {
                        services.TryAddEnumerable(ServiceDescriptor.Transient(iface, type));
                    }
                }
            }
        }

        return services;
    }
}
