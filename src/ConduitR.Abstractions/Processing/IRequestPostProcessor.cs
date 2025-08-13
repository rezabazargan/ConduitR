using System.Threading;
using System.Threading.Tasks;

namespace ConduitR.Abstractions;

/// <summary>Executes logic after a request handler runs.</summary>
public interface IRequestPostProcessor<in TRequest, in TResponse>
    where TRequest : IRequest<TResponse>
{
    Task Process(TRequest request, TResponse response, CancellationToken cancellationToken);
}
