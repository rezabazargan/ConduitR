using System.Diagnostics;
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

        using var activity = ConduitRTelemetry.ActivitySource.StartActivity("Mediator.Publish", ActivityKind.Internal);
        activity?.SetTag("conduitr.notification_type", typeof(TNotification).FullName);
        activity?.SetTag("conduitr.handlers.count", handlers.Length);

        var tasks = new List<Task>(handlers.Length);
        foreach (var handler in handlers)
        {
            var handlerType = handler.GetType();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            tasks.Add(InvokeHandler(handler, notification, cancellationToken, activity, handlerType, sw));
        }
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private static async Task InvokeHandler<TNotification>(INotificationHandler<TNotification> handler, TNotification notification, CancellationToken ct, Activity? parent, Type handlerType, System.Diagnostics.Stopwatch sw) where TNotification : INotification
    {
        try
        {
            await handler.Handle(notification, ct).ConfigureAwait(false);
            sw.Stop();
            parent?.AddEvent(new ActivityEvent("handler.completed", tags: new ActivityTagsCollection
            {
                { "conduitr.handler", handlerType.FullName ?? handlerType.Name },
                { "conduitr.elapsed_ms", sw.Elapsed.TotalMilliseconds }
            }));
        }
        catch (Exception ex)
        {
            sw.Stop();
            parent?.AddEvent(new ActivityEvent("handler.exception", tags: new ActivityTagsCollection
            {
                { "exception.type", ex.GetType().FullName },
                { "exception.message", ex.Message },
                { "conduitr.handler", handlerType.FullName ?? handlerType.Name },
                { "conduitr.elapsed_ms", sw.Elapsed.TotalMilliseconds }
            }));
            parent?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private interface IRequestHandlerWrapper<TResponse>
    {
        ValueTask<TResponse> Handle(IRequest<TResponse> request, CancellationToken ct, GetInstances getInstances);
    }

    private sealed class RequestHandlerWrapper<TRequest, TResponse> : IRequestHandlerWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async ValueTask<TResponse> Handle(IRequest<TResponse> request, CancellationToken ct, GetInstances getInstances)
        {
            var handlers = getInstances(typeof(IRequestHandler<TRequest, TResponse>))
                .Cast<IRequestHandler<TRequest, TResponse>>()
                .ToArray();

            if (handlers.Length == 0)
                throw new InvalidOperationException($"No handler registered for {typeof(TRequest).FullName}");

            if (handlers.Length > 1)
                throw new InvalidOperationException($"Multiple handlers ({handlers.Length}) registered for {typeof(TRequest).FullName}. Ensure a single handler or use distinct request types.");

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

            using var activity = ConduitRTelemetry.ActivitySource.StartActivity("Mediator.Send", ActivityKind.Internal);
            activity?.SetTag("conduitr.request_type", typeof(TRequest).FullName);
            activity?.SetTag("conduitr.response_type", typeof(TResponse).FullName);
            activity?.SetTag("conduitr.behaviors.count", behaviors.Length);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var result = await next().ConfigureAwait(false);
                sw.Stop();
                activity?.SetTag("conduitr.elapsed_ms", sw.Elapsed.TotalMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                activity?.SetTag("conduitr.elapsed_ms", sw.Elapsed.TotalMilliseconds);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
                {
                    { "exception.type", ex.GetType().FullName },
                    { "exception.message", ex.Message }
                }));
                throw;
            }
        }
    }
}
