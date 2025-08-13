using System.Threading;
using System.Threading.Tasks;

namespace ConduitR.Abstractions;

/// <summary>Delegate used by pipeline behaviors to invoke the next action.</summary>
/// <typeparam name="TResponse">Response type.</typeparam>
public delegate ValueTask<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>Pipeline behavior for cross-cutting concerns.</summary>
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next);
}
