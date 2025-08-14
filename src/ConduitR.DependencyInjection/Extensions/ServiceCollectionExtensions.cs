using ConduitR;
using ConduitR.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ConduitR.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConduit(this IServiceCollection services, Action<ConduitOptions>? configure = null)
    {
        var options = new ConduitOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(new MediatorOptions { PublishStrategy = options.PublishStrategy, EnableTelemetry = options.EnableTelemetry });

        services.AddScoped<ConduitR.Mediator>();
        services.AddScoped<IMediator>(sp => new ConduitR.Mediator(type => sp.GetServices(type)!.Cast<object>(), sp.GetRequiredService<MediatorOptions>()));

        foreach (var behaviorType in options.Behaviors)
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), behaviorType));
        }

        foreach (var asm in options.Assemblies.Distinct())
        {
            foreach (var type in asm.GetTypes().Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition))
            {
                foreach (var iface in type.GetInterfaces())
                {
                    if (!iface.IsGenericType) continue;
                    var def = iface.GetGenericTypeDefinition();
                    if (def == typeof(IRequestHandler<,>) || def == typeof(INotificationHandler<>) ||
                        def == typeof(IStreamRequestHandler<,>) || def == typeof(IStreamPipelineBehavior<,>))
                    {
                        services.TryAddEnumerable(ServiceDescriptor.Transient(iface, type));
                    }
                }
            }
        }

        return services;
    }
}
