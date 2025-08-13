using System.Reflection;
using ConduitR;

namespace ConduitR.DependencyInjection;

public sealed class ConduitOptions
{
    internal List<Assembly> Assemblies { get; } = new();
    internal List<Type> Behaviors { get; } = new();
    public PublishStrategy PublishStrategy { get; set; } = PublishStrategy.Parallel;

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
