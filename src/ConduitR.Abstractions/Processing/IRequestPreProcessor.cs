using System.Threading;
using System.Threading.Tasks;

namespace ConduitR.Abstractions;

/// <summary>Executes logic before a request handler runs.</summary>
public interface IRequestPreProcessor<in TRequest>
{
    Task Process(TRequest request, CancellationToken cancellationToken);
}
