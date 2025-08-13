using Polly;

namespace ConduitR.Resilience.Polly;

/// <summary>Provides a composed Polly policy for a given response type.</summary>
public interface IConduitPolicyProvider<TResponse>
{
    IAsyncPolicy<TResponse> GetPolicy();
}
