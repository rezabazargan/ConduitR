namespace ConduitR.Visualizer;

public sealed record RequestFlow(
    string RequestType,
    string ResponseType,
    string? HandlerType,
    IReadOnlyList<InvocationSite> InvocationSites,
    IReadOnlyList<PipelineBehaviorInfo> Pipeline,
    IReadOnlyList<HandlerDependencyInfo> Dependencies);
