using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using ConduitR.Abstractions;

namespace ConduitR;

public sealed partial class Mediator
{
    // Cache compiled Stream invokers per closed generic (TRequest,TResponse)
    private static readonly ConcurrentDictionary<(Type Req, Type Res), object> _streamInvokerCache = new();

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        using var activity = ConduitRTelemetry.ActivitySource.StartActivity("Mediator.Stream", ActivityKind.Internal);
        activity?.SetTag("conduitr.request_type", request.GetType().FullName);
        activity?.SetTag("conduitr.response_type", typeof(TResponse).FullName);

        var key = (request.GetType(), typeof(TResponse));

        var delObj = _streamInvokerCache.GetOrAdd(key, static k =>
        {
            var mi = typeof(Mediator).GetMethod(nameof(InvokeStream), BindingFlags.NonPublic | BindingFlags.Static)!;
            var g = mi.MakeGenericMethod(k.Req, k.Res);
            return g.CreateDelegate(typeof(StreamInvoker<>).MakeGenericType(k.Res));
        });

        var del = (StreamInvoker<TResponse>)delObj;
        return del(request, cancellationToken, _getInstances);
    }

    private delegate IAsyncEnumerable<TResponse> StreamInvoker<TResponse>(IStreamRequest<TResponse> request, CancellationToken ct, GetInstances getInstances);

    private static IAsyncEnumerable<TResponse> InvokeStream<TRequest, TResponse>(IStreamRequest<TResponse> request, CancellationToken ct, GetInstances getInstances)
        where TRequest : IStreamRequest<TResponse>
    {
        // Resolve single handler
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

        // Resolve behaviors
        var behaviors = new List<IStreamPipelineBehavior<TRequest, TResponse>>(capacity: 4);
        foreach (var obj in getInstances(typeof(IStreamPipelineBehavior<TRequest, TResponse>)))
        {
            if (obj is IStreamPipelineBehavior<TRequest, TResponse> b) behaviors.Add(b);
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
