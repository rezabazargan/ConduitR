using System.Reflection;
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

        if (options.Assemblies.Count == 0)
        {
            // you can choose a different default; explicit is better:
            // throw new InvalidOperationException("AddHandlersFromAssemblies(...) was not called.");
            options.Assemblies.Add(Assembly.GetCallingAssembly());
        }

        // 1) Register handlers found in configured assemblies
        RegisterHandlers(services, options.Assemblies);

        // 2) Register open-generic pipeline behaviors (if any)
        foreach (var openBehavior in options.Behaviors)
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), openBehavior));
        }

        // 3) Register Mediator via factory (so we can pass GetInstances) + IMediator alias
        services.TryAddScoped<IMediator>(sp => sp.GetRequiredService<Mediator>());
        services.TryAddScoped(sp =>
        {
            // ALWAYS return IEnumerable<object> (works for single & multi)
            Mediator.GetInstances get = (Type t) =>
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    var elem = t.GenericTypeArguments[0];
                    return sp.GetServices(elem); // IEnumerable<object> of elem
                }
                return sp.GetServices(t);      // IEnumerable<object> (0..n)
            };

            var medOpts = new MediatorOptions
            {
                PublishStrategy = options.PublishStrategy,
                EnableTelemetry = options.EnableTelemetry
            };

            return new Mediator(get, medOpts);
        });

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        static bool IsClosed(Type t) => !(t.IsGenericTypeDefinition || t.ContainsGenericParameters);

        var handlerDefs = new[]
        {
            typeof(IRequestHandler<,>),
            typeof(INotificationHandler<>),
            typeof(IStreamRequestHandler<,>)
        };

        foreach (var asm in assemblies)
        {
            foreach (var type in asm.DefinedTypes)
            {
                if (!type.IsClass || type.IsAbstract) continue;

                foreach (var itf in type.GetInterfaces())
                {
                    if (!itf.IsGenericType) continue;

                    var def = itf.GetGenericTypeDefinition();
                    if (!handlerDefs.Contains(def)) continue;
                    if (!IsClosed(itf)) continue; // only register closed generics

                    // Allow multiple registrations (e.g., many notification handlers)
                    services.TryAddEnumerable(ServiceDescriptor.Transient(itf, type));
                }
            }
        }
    }
}
