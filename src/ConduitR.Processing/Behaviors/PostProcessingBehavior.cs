using ConduitR.Abstractions;

namespace ConduitR.Processing;

/// <summary>Runs IRequestPostProcessor&lt;TRequest,TResponse&gt; instances after the handler.</summary>
public sealed class PostProcessingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IRequestPostProcessor<TRequest, TResponse>> _processors;
    public PostProcessingBehavior(IEnumerable<IRequestPostProcessor<TRequest, TResponse>> processors) => _processors = processors;

    public async ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        var response = await next().ConfigureAwait(false);
        if (_processors is not null)
        {
            foreach (var p in _processors)
                await p.Process(request, response, cancellationToken).ConfigureAwait(false);
        }
        return response;
    }
}
