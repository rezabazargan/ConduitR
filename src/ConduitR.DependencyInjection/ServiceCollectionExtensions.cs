using System.Reflection;
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

        // Register mediator
        services.AddScoped<ConduitR.Mediator>();
        services.AddScoped<IMediator>(sp => new ConduitR.Mediator(type => sp.GetServices(type)!.Cast<object>()));

        // Register open-generic behaviors configured by user
        foreach (var behaviorType in options.Behaviors)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), behaviorType);
        }

        // Scan and register handlers
        foreach (var asm in options.Assemblies.Distinct())
        {
            foreach (var type in asm.GetTypes().Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition))
            {
                foreach (var iface in type.GetInterfaces())
                {
                    if (iface.IsGenericType)
                    {
                        var def = iface.GetGenericTypeDefinition();
                        if (def == typeof(IRequestHandler<,>) || def == typeof(INotificationHandler<>))
                        {
                            services.TryAddEnumerable(ServiceDescriptor.Transient(iface, type));
                        }
                    }
                }
            }
        }

        return services;
    }
}

public sealed class ConduitOptions
{
    internal List<Assembly> Assemblies { get; } = new();
    internal List<Type> Behaviors { get; } = new();

    public ConduitOptions AddHandlersFromAssemblies(params Assembly[] assemblies)
    {
        Assemblies.AddRange(assemblies);
        return this;
    }

    public ConduitOptions AddBehavior(Type openGenericBehaviorType)
    {
        if (!openGenericBehaviorType.IsGenericTypeDefinition)
            throw new ArgumentException("Behavior type must be open generic, e.g. typeof(YourBehavior<,>)", nameof(openGenericBehaviorType));

        Behaviors.Add(openGenericBehaviorType);
        return this;
    }
}
