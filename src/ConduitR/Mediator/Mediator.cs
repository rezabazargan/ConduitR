using System.Collections.Concurrent;
using System.Diagnostics;
using ConduitR.Abstractions;
using ConduitR.Internal;

namespace ConduitR;

/// <summary>Resolves handlers and orchestrates pipeline behaviors.</summary>
public sealed partial class Mediator : IMediator
{
    /// <summary>Factory delegate used to resolve services (handlers/behaviors) from DI.</summary>
    public delegate IEnumerable<object> GetInstances(Type serviceType);

    private readonly GetInstances _getInstances;

    // Cache of stateless handler wrappers keyed by (request type, response type)
    private static readonly ConcurrentDictionary<(Type Req, Type Res), object> _wrapperCache = new();

    public Mediator(GetInstances getInstances)
    {
        _getInstances = getInstances ?? throw new ArgumentNullException(nameof(getInstances));
    }

    public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        using var activity = ConduitRTelemetry.ActivitySource.StartActivity("Mediator.Send", ActivityKind.Internal);
        activity?.SetTag("conduitr.request_type", request.GetType().FullName);
        activity?.SetTag("conduitr.response_type", typeof(TResponse).FullName);

        var key = (request.GetType(), typeof(TResponse));
        // Wrapper is stateless, safe to cache one instance per closed generic
        var wrapperObj = _wrapperCache.GetOrAdd(key, static k =>
        {
            var wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(k.Req, k.Res);
            return Activator.CreateInstance(wrapperType)!;
        });

        var wrapper = (IRequestHandlerWrapper<TResponse>)wrapperObj;
        return wrapper.Handle(request, cancellationToken, t => _getInstances(t));
    }

    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (notification is null) throw new ArgumentNullException(nameof(notification));

        // Enumerate handlers with minimal allocations
        var instances = _getInstances(typeof(INotificationHandler<TNotification>));
        if (instances is null) return;

        // Build a local list; avoid LINQ allocations
        var handlers = new List<INotificationHandler<TNotification>>(4);
        foreach (var obj in instances)
        {
            if (obj is INotificationHandler<TNotification> h) handlers.Add(h);
        }
        if (handlers.Count == 0) return;

        using var activity = ConduitRTelemetry.ActivitySource.StartActivity("Mediator.Publish", ActivityKind.Internal);
        activity?.SetTag("conduitr.notification_type", typeof(TNotification).FullName);
        activity?.SetTag("conduitr.handlers.count", handlers.Count);

        // Execute all handlers
        var tasks = new Task[handlers.Count];
        for (int i = 0; i < handlers.Count; i++)
        {
            var handler = handlers[i];
            var handlerType = handler.GetType();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            tasks[i] = InvokeHandler(handler, notification, cancellationToken, activity, handlerType, sw);
        }

        if (tasks.Length == 1)
        {
            await tasks[0].ConfigureAwait(false);
        }
        else
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
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
}
