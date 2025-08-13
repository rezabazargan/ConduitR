using System.Collections.Concurrent;
using System.Diagnostics;
using ConduitR.Abstractions;
using ConduitR.Internal;

namespace ConduitR;

public sealed partial class Mediator : IMediator
{
    public delegate IEnumerable<object> GetInstances(Type serviceType);

    private readonly GetInstances _getInstances;
    private readonly MediatorOptions _options;

    private static readonly ConcurrentDictionary<(Type Req, Type Res), object> _wrapperCache = new();

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

        var instances = _getInstances(typeof(INotificationHandler<TNotification>));
        if (instances is null) return;

        var handlers = new List<INotificationHandler<TNotification>>(4);
        foreach (var obj in instances)
        {
            if (obj is INotificationHandler<TNotification> h) handlers.Add(h);
        }
        if (handlers.Count == 0) return;

        using var activity = ConduitRTelemetry.ActivitySource.StartActivity("Mediator.Publish", ActivityKind.Internal);
        activity?.SetTag("conduitr.notification_type", typeof(TNotification).FullName);
        activity?.SetTag("conduitr.handlers.count", handlers.Count);
        activity?.SetTag("conduitr.publish_strategy", _options.PublishStrategy.ToString());

        switch (_options.PublishStrategy)
        {
            case PublishStrategy.Parallel:
                await PublishParallel(handlers, notification, cancellationToken, activity).ConfigureAwait(false);
                break;
            case PublishStrategy.Sequential:
                await PublishSequential(handlers, notification, cancellationToken, activity, stopOnFirstException: false).ConfigureAwait(false);
                break;
            case PublishStrategy.StopOnFirstException:
                await PublishSequential(handlers, notification, cancellationToken, activity, stopOnFirstException: true).ConfigureAwait(false);
                break;
        }
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

        if (tasks.Length == 1)
            await tasks[0].ConfigureAwait(false);
        else
            await Task.WhenAll(tasks).ConfigureAwait(false);
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
