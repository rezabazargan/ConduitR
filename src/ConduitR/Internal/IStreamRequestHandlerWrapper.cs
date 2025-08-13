using System.Collections.Generic;
using ConduitR.Abstractions;

namespace ConduitR.Internal;

internal interface IStreamRequestHandlerWrapper<TResponse>
{
    IAsyncEnumerable<TResponse> Handle(IStreamRequest<TResponse> request, CancellationToken ct, Func<Type, IEnumerable<object>> getInstances);
}
