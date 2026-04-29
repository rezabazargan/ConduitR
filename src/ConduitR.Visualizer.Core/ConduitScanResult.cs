namespace ConduitR.Visualizer;

internal sealed record ConduitScanResult(
    IReadOnlyList<TypeLocation> Types,
    IReadOnlyList<RequestRegistration> Requests,
    IReadOnlyList<RequestHandlerRegistration> RequestHandlers,
    IReadOnlyList<NotificationRegistration> Notifications,
    IReadOnlyList<NotificationHandlerRegistration> NotificationHandlers,
    IReadOnlyList<StreamRegistration> Streams,
    IReadOnlyList<StreamHandlerRegistration> StreamHandlers,
    IReadOnlyList<InvocationSite> InvocationSites,
    IReadOnlyList<PipelineBehaviorInfo> Behaviors);
