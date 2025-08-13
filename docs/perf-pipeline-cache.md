# Pipeline delegate caching (Send & Stream)

ConduitR caches compiled **invoker delegates** per `(TRequest, TResponse)` for both `Send` and `Stream`.
Each invoker avoids reflection/Activator on the hot path while still resolving handler/behavior instances
from DI on every call (so lifetimes are respected). No public API changes required.
