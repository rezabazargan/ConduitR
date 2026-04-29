namespace ConduitR.Visualizer;

public sealed record InvocationSite(
    string Kind,
    string FilePath,
    int Line,
    string Expression);
