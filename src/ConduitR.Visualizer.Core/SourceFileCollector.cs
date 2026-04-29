using System.Xml.Linq;

namespace ConduitR.Visualizer;

internal static class SourceFileCollector
{
    public static async Task<IReadOnlyList<SourceFile>> CollectAsync(string targetPath, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(targetPath);
        var projectFiles = extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)
            ? await ReadProjectsFromSolutionAsync(targetPath, cancellationToken).ConfigureAwait(false)
            : await ExpandProjectReferencesAsync(new[] { targetPath }, cancellationToken).ConfigureAwait(false);

        var sourcePaths = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var projectFile in projectFiles)
        {
            var projectDirectory = Path.GetDirectoryName(projectFile);
            if (string.IsNullOrWhiteSpace(projectDirectory) || !Directory.Exists(projectDirectory))
            {
                continue;
            }

            foreach (var sourcePath in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
            {
                if (IsGeneratedOrBuildOutput(sourcePath)) continue;
                sourcePaths.Add(Path.GetFullPath(sourcePath));
            }
        }

        var files = new List<SourceFile>(sourcePaths.Count);
        foreach (var sourcePath in sourcePaths)
        {
            var text = await File.ReadAllTextAsync(sourcePath, cancellationToken).ConfigureAwait(false);
            files.Add(new SourceFile(sourcePath, text));
        }

        return files;
    }

    private static async Task<IReadOnlyList<string>> ReadProjectsFromSolutionAsync(string solutionPath, CancellationToken cancellationToken)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath) ?? Directory.GetCurrentDirectory();
        var lines = await File.ReadAllLinesAsync(solutionPath, cancellationToken).ConfigureAwait(false);
        var projects = new List<string>();

        foreach (var line in lines)
        {
            var parts = line.Split(',');
            if (parts.Length < 2) continue;

            var rawProjectPath = parts[1].Trim().Trim('"');
            if (!rawProjectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)) continue;

            projects.Add(Path.GetFullPath(Path.Combine(solutionDirectory, rawProjectPath)));
        }

        return await ExpandProjectReferencesAsync(projects, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<string>> ExpandProjectReferencesAsync(
        IReadOnlyList<string> projectFiles,
        CancellationToken cancellationToken)
    {
        var discovered = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<string>(projectFiles.Select(Path.GetFullPath));

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var projectFile = pending.Dequeue();
            if (!File.Exists(projectFile) || !discovered.Add(projectFile))
            {
                continue;
            }

            foreach (var reference in await ReadProjectReferencesAsync(projectFile, cancellationToken).ConfigureAwait(false))
            {
                pending.Enqueue(reference);
            }
        }

        return discovered.ToArray();
    }

    private static async Task<IReadOnlyList<string>> ReadProjectReferencesAsync(
        string projectFile,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(projectFile);
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
        var projectDirectory = Path.GetDirectoryName(projectFile) ?? Directory.GetCurrentDirectory();

        return document
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.GetFullPath(Path.Combine(projectDirectory, value!)))
            .ToArray();
    }

    private static bool IsGeneratedOrBuildOutput(string sourcePath)
    {
        var normalized = sourcePath.Replace('\\', '/');
        return normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(".AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/.vs/", StringComparison.OrdinalIgnoreCase);
    }
}
