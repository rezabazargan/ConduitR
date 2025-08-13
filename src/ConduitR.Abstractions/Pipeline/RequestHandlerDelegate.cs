using System.Threading.Tasks;

namespace ConduitR.Abstractions;

/// <summary>Delegate used by pipeline behaviors to invoke the next action in the chain.</summary>
public delegate ValueTask<TResponse> RequestHandlerDelegate<TResponse>();
