namespace ConduitR.Visualizer;

public interface IConduitSolutionScanner
{
    Task<ConduitFlow> ScanAsync(string targetPath, CancellationToken cancellationToken = default);
}
