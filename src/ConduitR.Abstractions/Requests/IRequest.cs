namespace ConduitR.Abstractions;

/// <summary>Marker interface for a request with a response.</summary>
/// <typeparam name="TResponse">Response type.</typeparam>
public interface IRequest<out TResponse> {}
