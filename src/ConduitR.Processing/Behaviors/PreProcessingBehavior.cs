using ConduitR.Abstractions;

namespace ConduitR.Processing;

/// <summary>Runs IRequestPreProcessor&lt;TRequest&gt; instances before the handler.</summary>
public sealed class PreProcessingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IRequestPreProcessor<TRequest>> _processors;
    public PreProcessingBehavior(IEnumerable<IRequestPreProcessor<TRequest>> processors) => _processors = processors;

    public async ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        if (_processors is not null)
        {
            foreach (var p in _processors)
                await p.Process(request, cancellationToken).ConfigureAwait(false);
        }
        return await next().ConfigureAwait(false);
    }
}
