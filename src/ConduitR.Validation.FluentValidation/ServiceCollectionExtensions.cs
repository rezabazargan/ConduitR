using System.Reflection;
using ConduitR.Abstractions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ConduitR.Validation.FluentValidation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConduitValidation(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)));

        if (assemblies is null || assemblies.Length == 0) return services;

        var openValidator = typeof(IValidator<>);
        foreach (var asm in assemblies.Distinct())
        {
            foreach (var type in asm.GetTypes().Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition))
            {
                foreach (var iface in type.GetInterfaces())
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == openValidator)
                        services.TryAddEnumerable(ServiceDescriptor.Transient(iface, type));
                }
            }
        }
        return services;
    }
}
