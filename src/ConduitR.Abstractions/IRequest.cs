namespace ConduitR.Abstractions;

/// <summary>Marker interface for a request with a response.</summary>
/// <typeparam name="TResponse">Response type.</typeparam>
public interface IRequest<out TResponse> {}

/// <summary>Represents a unit type (void) for requests without a return value.</summary>
public readonly struct Unit
{
    public static readonly Unit Value = new();
}
