namespace ConduitR.Visualizer;

public sealed record StreamFlow(
    string RequestType,
    string ResponseType,
    string? HandlerType,
    IReadOnlyList<InvocationSite> InvocationSites,
    IReadOnlyList<PipelineBehaviorInfo> Pipeline,
    IReadOnlyList<HandlerDependencyInfo> Dependencies);
