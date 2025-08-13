# ConduitR Roadmap

## ✅ Done
- Validation behavior (+ DI scan)
- AspNetCore helpers (ProblemDetails, minimal-API helper)
- Telemetry (ActivitySource spans)
- Streaming (`IStreamRequest<>` + handler; DI support)
- Perf: send-wrapper caching + lean publish path
- One-type-per-file refactor

## 🎯 Next (high-impact)
- [ ] **Cache composed pipelines per request type** (avoid rebuilding behavior chain every send)
- [ ] **Publish strategies**: Parallel (current), Sequential, StopOnFirstException + handler-list cache
- [ ] **Analyzers** (Roslyn): one handler per request, missing registration hints, cancellation usage
- [ ] **Resilience behaviors** (Polly): retry/timeout/circuit-breaker for Send/Stream
- [ ] **NuGet polish**: PackageReadmeFile in all packs; richer tags; validate repo links
- [ ] **Docs**: “Migrate from MediatR in 10 minutes”; API docs (DocFX)

## 📌 Nice-to-have
- [ ] Source generator to precompute handler maps/pipelines
- [ ] Caching behavior (IMemoryCache/IDistributedCache)
- [ ] OpenTelemetry attributes for request/handler names and outcomes
