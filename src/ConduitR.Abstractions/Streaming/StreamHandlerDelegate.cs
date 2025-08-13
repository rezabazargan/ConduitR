using System.Collections.Generic;

namespace ConduitR.Abstractions;

/// <summary>Delegate used by streaming pipeline behaviors to invoke the next handler.</summary>
public delegate IAsyncEnumerable<TResponse> StreamHandlerDelegate<TResponse>();
