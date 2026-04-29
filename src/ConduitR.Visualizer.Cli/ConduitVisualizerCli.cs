using ConduitR.Visualizer;

namespace ConduitR.Visualizer.Cli;

internal static class ConduitVisualizerCli
{
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            WriteHelp();
            return 0;
        }

        if (!string.Equals(args[0], "visualize", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"Unknown command: {args[0]}");
            WriteHelp();
            return 2;
        }

        return await RunVisualizeAsync(args.Skip(1).ToArray(), cancellationToken).ConfigureAwait(false);
    }

    private static async Task<int> RunVisualizeAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            WriteVisualizeHelp();
            return args.Length == 0 ? 2 : 0;
        }

        var solutionPath = args[0];
        var outputDirectory = "artifacts/conduitr";

        for (var i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "--output", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "-o", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                {
                    Console.Error.WriteLine("Missing value for --output.");
                    return 2;
                }

                outputDirectory = args[++i];
                continue;
            }

            Console.Error.WriteLine($"Unknown option: {arg}");
            WriteVisualizeHelp();
            return 2;
        }

        try
        {
            IConduitSolutionScanner scanner = new ConduitSolutionScanner();
            IConduitReportWriter writer = new ConduitReportWriter();

            var flow = await scanner.ScanAsync(solutionPath, cancellationToken).ConfigureAwait(false);
            await writer.WriteAsync(flow, outputDirectory, cancellationToken).ConfigureAwait(false);

            Console.WriteLine($"ConduitR Visualizer report written to: {Path.GetFullPath(outputDirectory)}");
            Console.WriteLine("- flows.md");
            Console.WriteLine("- flows.json");
            Console.WriteLine("- diagrams/*.mmd");
            return 0;
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Visualizer failed: {ex.Message}");
            return 1;
        }
    }

    private static bool IsHelp(string value) =>
        string.Equals(value, "--help", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "-h", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "help", StringComparison.OrdinalIgnoreCase);

    private static void WriteHelp()
    {
        Console.WriteLine("ConduitR Visualizer");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  conduitr visualize <solution-or-project> [--output <directory>]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  visualize    Generate ConduitR flow artifacts for a solution or project.");
    }

    private static void WriteVisualizeHelp()
    {
        Console.WriteLine("Generate ConduitR flow artifacts for a solution or project.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  conduitr visualize <solution-or-project> [--output <directory>]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -o, --output <directory>    Output directory. Defaults to artifacts/conduitr.");
    }
}
