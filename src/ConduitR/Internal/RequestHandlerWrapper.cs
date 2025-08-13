using ConduitR.Abstractions;

namespace ConduitR.Internal;

/// <summary>Internal wrapper that resolves handlers & behaviors and executes the pipeline.</summary>
internal sealed class RequestHandlerWrapper<TRequest, TResponse> : IRequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    public ValueTask<TResponse> Handle(IRequest<TResponse> request, CancellationToken ct, Func<Type, IEnumerable<object>> getInstances)
    {
        var handlers = getInstances(typeof(IRequestHandler<TRequest, TResponse>))
            .Cast<IRequestHandler<TRequest, TResponse>>()
            .ToArray();

        if (handlers.Length == 0)
            throw new InvalidOperationException($"No handler registered for {typeof(TRequest).FullName}");

        if (handlers.Length > 1)
            throw new InvalidOperationException($"Multiple handlers ({handlers.Length}) registered for {typeof(TRequest).FullName}. Ensure a single handler or distinct request types.");

        var handler = handlers[0];

        var behaviors = getInstances(typeof(IPipelineBehavior<TRequest, TResponse>))
            .Cast<IPipelineBehavior<TRequest, TResponse>>()
            .ToArray();

        var typedRequest = (TRequest)request;

        RequestHandlerDelegate<TResponse> next = () => handler.Handle(typedRequest, ct);

        // Compose behaviors in reverse registration order (last-added runs first)
        for (int i = behaviors.Length - 1; i >= 0; i--)
        {
            var current = behaviors[i];
            var nextCopy = next;
            next = () => current.Handle(typedRequest, ct, nextCopy);
        }

        return next();
    }
}
