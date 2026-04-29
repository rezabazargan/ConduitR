namespace ConduitR.Visualizer;

public interface IConduitReportWriter
{
    Task WriteAsync(ConduitFlow flow, string outputDirectory, CancellationToken cancellationToken = default);
}
