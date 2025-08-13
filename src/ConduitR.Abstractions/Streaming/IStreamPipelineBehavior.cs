using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConduitR.Abstractions;

/// <summary>Pipeline behavior for streaming requests.</summary>
public interface IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken, StreamHandlerDelegate<TResponse> next);
}
