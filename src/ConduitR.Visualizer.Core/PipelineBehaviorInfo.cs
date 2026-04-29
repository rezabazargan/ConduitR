namespace ConduitR.Visualizer;

public sealed record PipelineBehaviorInfo(
    string BehaviorType,
    int Order,
    string Source,
    string ClassName = "",
    SourceLocation? ClassLocation = null);
