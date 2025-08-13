using ConduitR.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ConduitR.DependencyInjection;

/// <summary>DI extensions for ConduitR.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConduit(this IServiceCollection services, Action<ConduitOptions>? configure = null)
    {
        var options = new ConduitOptions();
        configure?.Invoke(options);

        // Register mediator
        services.AddScoped<ConduitR.Mediator>();
        services.AddScoped<IMediator>(sp => new ConduitR.Mediator(type => sp.GetServices(type)!.Cast<object>()));

        // Register user-specified open generic behaviors
        foreach (var behaviorType in options.Behaviors)
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), behaviorType));
        }

        // Scan and register handlers (dedupe with TryAddEnumerable)
        foreach (var asm in options.Assemblies.Distinct())
        {
            foreach (var type in asm.GetTypes().Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition))
            {
                foreach (var iface in type.GetInterfaces())
                {
                    if (!iface.IsGenericType) continue;
                    var def = iface.GetGenericTypeDefinition();
                    if (def == typeof(IRequestHandler<,>) || def == typeof(INotificationHandler<>))
                    {
                        services.TryAddEnumerable(ServiceDescriptor.Transient(iface, type));
                    }
                }
            }
        }

        return services;
    }
}
