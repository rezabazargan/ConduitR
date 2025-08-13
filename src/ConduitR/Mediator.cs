using System.Reflection;
using ConduitR.Abstractions;

namespace ConduitR;

/// <summary>Resolves handlers and orchestrates pipeline behaviors.</summary>
public sealed class Mediator : IMediator
{
    public delegate IEnumerable<object> GetInstances(Type serviceType);

    private readonly GetInstances _getInstances;

    public Mediator(GetInstances getInstances)
    {
        _getInstances = getInstances ?? throw new ArgumentNullException(nameof(getInstances));
    }

    public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        var wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var wrapper = (IRequestHandlerWrapper<TResponse>)Activator.CreateInstance(wrapperType)!;
        return wrapper.Handle(request, cancellationToken, _getInstances);
    }

    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (notification is null) throw new ArgumentNullException(nameof(notification));
        var handlers = _getInstances(typeof(INotificationHandler<TNotification>))
            .Cast<INotificationHandler<TNotification>>()
            .ToArray();

        if (handlers.Length == 0) return;

        var tasks = new List<Task>(handlers.Length);
        foreach (var handler in handlers)
        {
            tasks.Add(handler.Handle(notification, cancellationToken));
        }
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private interface IRequestHandlerWrapper<TResponse>
    {
        ValueTask<TResponse> Handle(IRequest<TResponse> request, CancellationToken ct, GetInstances getInstances);
    }

    private sealed class RequestHandlerWrapper<TRequest, TResponse> : IRequestHandlerWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public ValueTask<TResponse> Handle(IRequest<TResponse> request, CancellationToken ct, GetInstances getInstances)
        {
            var handler = getInstances(typeof(IRequestHandler<TRequest, TResponse>))
                .Cast<IRequestHandler<TRequest, TResponse>>()
                .SingleOrDefault() ?? throw new InvalidOperationException($"No handler registered for {typeof(TRequest).FullName}");

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
}
