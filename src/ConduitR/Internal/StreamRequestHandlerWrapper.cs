using System.Collections.Generic;
using ConduitR.Abstractions;

namespace ConduitR.Internal;

internal sealed class StreamRequestHandlerWrapper<TRequest, TResponse> : IStreamRequestHandlerWrapper<TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    public IAsyncEnumerable<TResponse> Handle(IStreamRequest<TResponse> request, CancellationToken ct, Func<Type, IEnumerable<object>> getInstances)
    {
        var handlers = getInstances(typeof(IStreamRequestHandler<TRequest, TResponse>))
            .Cast<IStreamRequestHandler<TRequest, TResponse>>()
            .ToArray();

        if (handlers.Length == 0)
            throw new InvalidOperationException($"No stream handler registered for {typeof(TRequest).FullName}");
        if (handlers.Length > 1)
            throw new InvalidOperationException($"Multiple stream handlers ({handlers.Length}) registered for {typeof(TRequest).FullName}. Ensure a single handler.");

        var handler = handlers[0];

        var behaviors = getInstances(typeof(IStreamPipelineBehavior<TRequest, TResponse>))
            .Cast<IStreamPipelineBehavior<TRequest, TResponse>>()
            .ToArray();

        var typedRequest = (TRequest)request;

        StreamHandlerDelegate<TResponse> next = () => handler.Handle(typedRequest, ct);

        for (int i = behaviors.Length - 1; i >= 0; i--)
        {
            var current = behaviors[i];
            var nextCopy = next;
            next = () => current.Handle(typedRequest, ct, nextCopy);
        }

        return next();
    }
}
