using ConduitR.Abstractions;

namespace ConduitR.Internal;

internal sealed class RequestHandlerWrapper<TRequest, TResponse> : IRequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    public ValueTask<TResponse> Handle(IRequest<TResponse> request, CancellationToken ct, Func<Type, IEnumerable<object>> getInstances)
    {
        // Resolve single handler without LINQ/arrays
        IRequestHandler<TRequest, TResponse>? handler = null;
        foreach (var obj in getInstances(typeof(IRequestHandler<TRequest, TResponse>)))
        {
            if (obj is IRequestHandler<TRequest, TResponse> h)
            {
                if (handler is not null)
                    throw new InvalidOperationException($"Multiple handlers registered for {typeof(TRequest).FullName}");
                handler = h;
            }
        }
        if (handler is null)
            throw new InvalidOperationException($"No handler registered for {typeof(TRequest).FullName}");

        // Gather behaviors without ToArray
        List<IPipelineBehavior<TRequest, TResponse>> behaviors = new(capacity: 4);
        foreach (var obj in getInstances(typeof(IPipelineBehavior<TRequest, TResponse>)))
        {
            if (obj is IPipelineBehavior<TRequest, TResponse> b)
                behaviors.Add(b);
        }

        var typedRequest = (TRequest)request;

        RequestHandlerDelegate<TResponse> next = () => handler.Handle(typedRequest, ct);

        // Compose in reverse
        for (int i = behaviors.Count - 1; i >= 0; i--)
        {
            var current = behaviors[i];
            var nextCopy = next;
            next = () => current.Handle(typedRequest, ct, nextCopy);
        }

        return next();
    }
}
