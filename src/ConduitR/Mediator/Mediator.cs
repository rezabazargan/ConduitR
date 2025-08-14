using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using ConduitR.Abstractions;

namespace ConduitR;

/// <summary>Resolves handlers and orchestrates pipeline behaviors.</summary>
public sealed partial class Mediator : IMediator
{
    /// <summary>Factory delegate used to resolve services (handlers/behaviors) from DI.</summary>
    public delegate IEnumerable<object> GetInstances(Type serviceType);

    private readonly GetInstances _getInstances;
    private readonly MediatorOptions _options;

    // Cache compiled Send invokers per closed generic (TRequest,TResponse)
    private static readonly ConcurrentDictionary<(Type Req, Type Res), object> _sendInvokerCache = new();

    public Mediator(GetInstances getInstances, MediatorOptions? options = null)
    {
        _getInstances = getInstances ?? throw new ArgumentNullException(nameof(getInstances));
        _options = options ?? new MediatorOptions();
    }

    public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        using var activity = ConduitRTelemetry.ActivitySource.StartActivity("Mediator.Send", ActivityKind.Internal);
        activity?.SetTag("conduitr.request_type", request.GetType().FullName);
        activity?.SetTag("conduitr.response_type", typeof(TResponse).FullName);

        var key = (request.GetType(), typeof(TResponse));

        // Get or compile the invoker delegate for this closed generic
        var delObj = _sendInvokerCache.GetOrAdd(key, static k =>
        {
            var mi = typeof(Mediator).GetMethod(nameof(InvokeSend), BindingFlags.NonPublic | BindingFlags.Static)!;
            var g = mi.MakeGenericMethod(k.Req, k.Res);
            return g.CreateDelegate(typeof(SendInvoker<>).MakeGenericType(k.Res));
        });

        var del = (SendInvoker<TResponse>)delObj;
        return del(request, cancellationToken, _getInstances);
    }

    // Invoker signature (generic over TResponse only for cache storage)
    private delegate ValueTask<TResponse> SendInvoker<TResponse>(IRequest<TResponse> request, CancellationToken ct, GetInstances getInstances);

    // Composes pipeline for specific TRequest/TResponse and executes it
    private static ValueTask<TResponse> InvokeSend<TRequest, TResponse>(IRequest<TResponse> request, CancellationToken ct, GetInstances getInstances)
        where TRequest : IRequest<TResponse>
    {
        // Resolve single handler w/out LINQ
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

        // Resolve behaviors
        var behaviors = new List<IPipelineBehavior<TRequest, TResponse>>(capacity: 4);
        foreach (var obj in getInstances(typeof(IPipelineBehavior<TRequest, TResponse>)))
        {
            if (obj is IPipelineBehavior<TRequest, TResponse> b) behaviors.Add(b);
        }

        var typedRequest = (TRequest)request;
        RequestHandlerDelegate<TResponse> next = () => handler.Handle(typedRequest, ct);

        for (int i = behaviors.Count - 1; i >= 0; i--)
        {
            var current = behaviors[i];
            var nextCopy = next;
            next = () => current.Handle(typedRequest, ct, nextCopy);
        }

        return next();
    }

    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (notification is null) throw new ArgumentNullException(nameof(notification));

        // Enumerate handlers with minimal allocations
        var instances = _getInstances(typeof(INotificationHandler<TNotification>));
        if (instances is null) return;

        var handlers = new List<INotificationHandler<TNotification>>(4);
        foreach (var obj in instances)
        {
            if (obj is INotificationHandler<TNotification> h) handlers.Add(h);
        }
        if (handlers.Count == 0) return;

        if (_options.EnableTelemetry)
        {
            using var activity = ConduitRTelemetry.ActivitySource.StartActivity("Mediator.Publish", ActivityKind.Internal);
            activity?.SetTag("conduitr.notification_type", typeof(TNotification).FullName);
            activity?.SetTag("conduitr.handlers.count", handlers.Count);
            activity?.SetTag("conduitr.publish_strategy", _options.PublishStrategy.ToString());
        }

        var strategy = _options.PublishStrategy;
        await Internal.PublishInvoker.Cache<TNotification>
            .Invoke(notification, strategy, cancellationToken, _getInstances)
            .ConfigureAwait(false);
    }

    private static async Task PublishParallel<TNotification>(IReadOnlyList<INotificationHandler<TNotification>> handlers, TNotification notification, CancellationToken ct, Activity? activity)
        where TNotification : INotification
    {
        var tasks = new Task[handlers.Count];
        for (int i = 0; i < handlers.Count; i++)
        {
            var handler = handlers[i];
            var handlerType = handler.GetType();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            tasks[i] = InvokeHandler(handler, notification, ct, activity, handlerType, sw);
        }

        if (tasks.Length == 1) await tasks[0].ConfigureAwait(false);
        else await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private static async Task PublishSequential<TNotification>(IReadOnlyList<INotificationHandler<TNotification>> handlers, TNotification notification, CancellationToken ct, Activity? activity, bool stopOnFirstException)
        where TNotification : INotification
    {
        List<Exception>? errors = null;
        for (int i = 0; i < handlers.Count; i++)
        {
            var handler = handlers[i];
            var handlerType = handler.GetType();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await handler.Handle(notification, ct).ConfigureAwait(false);
                sw.Stop();
                activity?.AddEvent(new ActivityEvent("handler.completed", tags: new ActivityTagsCollection
                {
                    { "conduitr.handler", handlerType.FullName ?? handlerType.Name },
                    { "conduitr.elapsed_ms", sw.Elapsed.TotalMilliseconds }
                }));
            }
            catch (Exception ex)
            {
                sw.Stop();
                activity?.AddEvent(new ActivityEvent("handler.exception", tags: new ActivityTagsCollection
                {
                    { "exception.type", ex.GetType().FullName },
                    { "exception.message", ex.Message },
                    { "conduitr.handler", handlerType.FullName ?? handlerType.Name },
                    { "conduitr.elapsed_ms", sw.Elapsed.TotalMilliseconds }
                }));
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                if (stopOnFirstException) throw;
                (errors ??= new List<Exception>(2)).Add(ex);
            }
        }

        if (errors is { Count: > 0 }) throw new AggregateException(errors);
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
