using System;

namespace ConduitR.Resilience.Polly;

/// <summary>Global resilience options applied to all ConduitR requests.</summary>
public sealed class ConduitResilienceOptions
{
    /// <summary>Number of retries on exception. 0 disables retries.</summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>Exponential backoff factory. Defaults to 100ms * 2^(attempt-1).</summary>
    public Func<int, TimeSpan> RetryBackoff { get; set; } = attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt - 1));

    /// <summary>Optional timeout for a single attempt. Null disables timeout strategy.</summary>
    public TimeSpan? Timeout { get; set; } = null;

    /// <summary>Enable circuit breaker on exceptions.</summary>
    public bool CircuitBreakerEnabled { get; set; } = false;

    /// <summary>Allowed exceptions before breaking.</summary>
    public int CircuitBreakerFailures { get; set; } = 5;

    /// <summary>Duration of the break.</summary>
    public TimeSpan CircuitBreakerDuration { get; set; } = TimeSpan.FromSeconds(30);
}
