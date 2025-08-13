using System.Collections.Generic;
using System.Diagnostics;
using ConduitR.Abstractions;
using ConduitR.Internal;

namespace ConduitR;

public sealed partial class Mediator
{
    /// <summary>Create a stream for the given streaming request.</summary>
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        using var activity = ConduitRTelemetry.ActivitySource.StartActivity("Mediator.Stream", ActivityKind.Internal);
        activity?.SetTag("conduitr.request_type", request.GetType().FullName);
        activity?.SetTag("conduitr.response_type", typeof(TResponse).FullName);

        var wrapperType = typeof(StreamRequestHandlerWrapper<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var wrapper = (IStreamRequestHandlerWrapper<TResponse>)Activator.CreateInstance(wrapperType)!;

        return wrapper.Handle(request, cancellationToken, t => _getInstances(t));
    }
}
