using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;

namespace ConduitR.Resilience.Polly;

internal sealed class DefaultPolicyProvider<TResponse> : IConduitPolicyProvider<TResponse>
{
    private readonly ConduitResilienceOptions _options;
    private IAsyncPolicy<TResponse>? _cached;

    public DefaultPolicyProvider(IOptions<ConduitResilienceOptions> options) => _options = options.Value;

    public IAsyncPolicy<TResponse> GetPolicy() => _cached ??= Build();

    private IAsyncPolicy<TResponse> Build()
    {
        // Inner-most per-attempt policy
        IAsyncPolicy<TResponse> composed =
            _options.Timeout is TimeSpan to
                ? Policy.TimeoutAsync<TResponse>(to, TimeoutStrategy.Pessimistic) // <- works even if handler ignores CT
                : Policy.NoOpAsync<TResponse>();

        // Circuit breaker (around the inner)
        if (_options.CircuitBreakerEnabled && _options.CircuitBreakerFailures > 0)
        {
            var cb = Policy<TResponse>
                .Handle<Exception>()
                .CircuitBreakerAsync(_options.CircuitBreakerFailures, _options.CircuitBreakerDuration);
            composed = cb.WrapAsync(composed);
        }

        // Retry with backoff (outer-most)
        if (_options.RetryCount > 0)
        {
            var retry = Policy<TResponse>
                .Handle<Exception>()
                .WaitAndRetryAsync(_options.RetryCount, attempt => _options.RetryBackoff(attempt));
            composed = retry.WrapAsync(composed);
        }

        return composed;
    }
}
