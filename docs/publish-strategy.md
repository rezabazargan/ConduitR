# Publish Strategy

Configure how notifications run:

```csharp
services.AddConduit(cfg => {
    cfg.PublishStrategy = PublishStrategy.Sequential; // Parallel (default), Sequential, StopOnFirstException
});
```

- **Parallel** (default): all handlers run concurrently; exceptions are aggregated.
- **Sequential**: handlers run one-by-one; all are attempted; exceptions aggregated.
- **StopOnFirstException**: handlers run one-by-one; stop immediately on first exception.
