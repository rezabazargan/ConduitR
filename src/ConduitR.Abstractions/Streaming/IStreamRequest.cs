namespace ConduitR.Abstractions;

/// <summary>Marker interface for a request that yields a stream of <typeparamref name="TResponse"/>.</summary>
public interface IStreamRequest<out TResponse> {}
