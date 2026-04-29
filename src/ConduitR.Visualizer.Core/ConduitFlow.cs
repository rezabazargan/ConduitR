namespace ConduitR.Visualizer;

public sealed record ConduitFlow(
    string TargetPath,
    IReadOnlyList<RequestFlow> Requests,
    IReadOnlyList<NotificationFlow> Notifications,
    IReadOnlyList<StreamFlow> Streams,
    IReadOnlyList<VisualizerDiagnostic> Diagnostics)
{
    public static ConduitFlow Empty(string targetPath) =>
        new(
            targetPath,
            Array.Empty<RequestFlow>(),
            Array.Empty<NotificationFlow>(),
            Array.Empty<StreamFlow>(),
            Array.Empty<VisualizerDiagnostic>());
}
