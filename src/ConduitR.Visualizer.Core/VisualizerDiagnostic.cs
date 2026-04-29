namespace ConduitR.Visualizer;

public sealed record VisualizerDiagnostic(
    string Severity,
    string Message,
    string? FilePath = null,
    int? Line = null);
