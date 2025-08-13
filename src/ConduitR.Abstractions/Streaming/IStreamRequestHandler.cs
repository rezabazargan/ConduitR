using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConduitR.Abstractions;

/// <summary>Handler for a streaming request.</summary>
public interface IStreamRequestHandler<in TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
