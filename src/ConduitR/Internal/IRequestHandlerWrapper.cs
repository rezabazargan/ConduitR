using ConduitR.Abstractions;

namespace ConduitR.Internal;

/// <summary>Internal adapter that invokes the strongly-typed request handler.</summary>
internal interface IRequestHandlerWrapper<TResponse>
{
    ValueTask<TResponse> Handle(IRequest<TResponse> request, CancellationToken ct, Func<Type, IEnumerable<object>> getInstances);
}
