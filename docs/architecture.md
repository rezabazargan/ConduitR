# Architecture

## Hot path optimizations (v1.0.1+)
- Handler wrapper caching per closed generic `(TRequest,TResponse)` avoids reflection/Activator on repeat sends.
- Publish path uses zero-LINQ enumeration and a single task array allocation.
- Telemetry spans wrap Send/Publish/Stream paths with minimal overhead.
