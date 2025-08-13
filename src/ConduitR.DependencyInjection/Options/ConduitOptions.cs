using System.Reflection;

namespace ConduitR.DependencyInjection;

/// <summary>Options for ConduitR DI integration.</summary>
public sealed class ConduitOptions
{
    internal List<Assembly> Assemblies { get; } = new();
    internal List<Type> Behaviors { get; } = new();

    /// <summary>Add assemblies to scan for handlers and notification handlers.</summary>
    public ConduitOptions AddHandlersFromAssemblies(params Assembly[] assemblies)
    {
        Assemblies.AddRange(assemblies);
        return this;
    }

    /// <summary>Register an open-generic pipeline behavior type, e.g. typeof(YourBehavior&lt;,&gt;).</summary>
    public ConduitOptions AddBehavior(Type openGenericBehaviorType)
    {
        if (!openGenericBehaviorType.IsGenericTypeDefinition)
            throw new ArgumentException("Behavior type must be open generic, e.g. typeof(YourBehavior<,>)", nameof(openGenericBehaviorType));

        Behaviors.Add(openGenericBehaviorType);
        return this;
    }
}
