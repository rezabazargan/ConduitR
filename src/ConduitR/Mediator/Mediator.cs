using System.Diagnostics;
using ConduitR.Abstractions;
using ConduitR.Internal;

namespace ConduitR;

/// <summary>Resolves handlers and orchestrates pipeline behaviors.</summary>
public sealed class Mediator : IMediator
{
    /// <summary>Factory delegate used to resolve services (handlers/behaviors) from DI.</summary>
    public delegate IEnumerable<object> GetInstances(Type serviceType);

    private readonly GetInstances _getInstances;

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

        var wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var wrapper = (IRequestHandlerWrapper<TResponse>)Activator.CreateInstance(wrapperType)!;

        return wrapper.Handle(request, cancellationToken, t => _getInstances(t));
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
}
