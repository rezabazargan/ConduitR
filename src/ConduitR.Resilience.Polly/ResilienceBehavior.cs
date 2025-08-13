using ConduitR.Abstractions;
using Polly;

namespace ConduitR.Resilience.Polly;

/// <summary>Applies the configured Polly resilience pipeline around the request handler.</summary>
public sealed class ResilienceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IConduitPolicyProvider<TResponse> _provider;

    public ResilienceBehavior(IConduitPolicyProvider<TResponse> provider) => _provider = provider;

    public async ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        var policy = _provider.GetPolicy();
        return await policy.ExecuteAsync(async (ct) => await next().AsTask().ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
    }
}
