
### ✅ Done

* Validation behavior (+ DI scan)
* AspNetCore helpers (ProblemDetails, minimal-API helper)
* Telemetry (ActivitySource spans)
* Streaming (`IStreamRequest<>`, handler, behaviors plumbed)
* Perf: wrapper caching + lean publish path
* One-type-per-file refactor

### 🎯 Next (high-impact)

* [ ] **Cache composed pipelines per request type** (avoid rebuilding behavior chain every send)
* [ ] **Publish strategies**: Parallel (current), Sequential, StopOnFirstException (with handler list cache)
* [ ] **Add analyzers** (Roslyn): one handler per request, missing registration hints, cancellation use
* [ ] **Retry/Timeout/CB behaviors** (Polly) for Send/Stream
* [ ] **NuGet polish**: PackageReadmeFile in all packs; richer tags; repo URL verified everywhere
* [ ] **Docs**: “Migrate from MediatR in 10 minutes”, API docs (DocFX)

