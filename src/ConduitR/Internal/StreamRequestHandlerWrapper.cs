using System.Collections.Generic;
using ConduitR.Abstractions;

namespace ConduitR.Internal;

internal sealed class StreamRequestHandlerWrapper<TRequest, TResponse> : IStreamRequestHandlerWrapper<TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    public IAsyncEnumerable<TResponse> Handle(IStreamRequest<TResponse> request, CancellationToken ct, Func<Type, IEnumerable<object>> getInstances)
    {
        IStreamRequestHandler<TRequest, TResponse>? handler = null;
        foreach (var obj in getInstances(typeof(IStreamRequestHandler<TRequest, TResponse>)))
        {
            if (obj is IStreamRequestHandler<TRequest, TResponse> h)
            {
                if (handler is not null)
                    throw new InvalidOperationException($"Multiple stream handlers registered for {typeof(TRequest).FullName}");
                handler = h;
            }
        }
        if (handler is null)
            throw new InvalidOperationException($"No stream handler registered for {typeof(TRequest).FullName}");

        List<IStreamPipelineBehavior<TRequest, TResponse>> behaviors = new(capacity: 4);
        foreach (var obj in getInstances(typeof(IStreamPipelineBehavior<TRequest, TResponse>)))
        {
            if (obj is IStreamPipelineBehavior<TRequest, TResponse> b)
                behaviors.Add(b);
        }

        var typedRequest = (TRequest)request;

        StreamHandlerDelegate<TResponse> next = () => handler.Handle(typedRequest, ct);

        for (int i = behaviors.Count - 1; i >= 0; i--)
        {
            var current = behaviors[i];
            var nextCopy = next;
            next = () => current.Handle(typedRequest, ct, nextCopy);
        }

        return next();
    }
}
