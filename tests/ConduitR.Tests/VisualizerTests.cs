using ConduitR.Visualizer;
using Xunit;

public sealed class VisualizerTests
{
    [Fact]
    public async Task Scanner_throws_when_solution_is_missing()
    {
        var scanner = new ConduitSolutionScanner();

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            scanner.ScanAsync(Path.Combine(Path.GetTempPath(), "missing-conduitr.sln")));
    }

    [Fact]
    public async Task Report_writer_creates_markdown_and_json_artifacts()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "conduitr-visualizer-tests", Guid.NewGuid().ToString("N"));
        var flow = ConduitFlow.Empty(Path.GetFullPath("ConduitR.sln"));
        var writer = new ConduitReportWriter();

        try
        {
            await writer.WriteAsync(flow, outputDirectory);

            Assert.True(File.Exists(Path.Combine(outputDirectory, "flows.md")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "flows.json")));
            Assert.True(Directory.Exists(Path.Combine(outputDirectory, "diagrams")));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Scanner_discovers_sample_web_api_flows()
    {
        var projectPath = Path.Combine(GetRepositoryRoot(), "samples", "Samples.WebApi", "Samples.WebApi.csproj");
        var scanner = new ConduitSolutionScanner();

        var flow = await scanner.ScanAsync(projectPath);

        var getTime = Assert.Single(flow.Requests, request => request.RequestType == "GetTimeQuery");
        Assert.Equal("GetTimeHandler", getTime.HandlerType);
        Assert.Contains(getTime.InvocationSites, site => site.Kind == "Send");
        Assert.Contains(getTime.Pipeline, behavior =>
            behavior.BehaviorType == "ValidationBehavior<GetTimeQuery, string>" &&
            behavior.ClassName == "ValidationBehavior" &&
            behavior.ClassLocation is not null);

        var echo = Assert.Single(flow.Requests, request => request.RequestType == "Echo");
        Assert.Contains(echo.InvocationSites, site => site.Kind == "MapMediatorPost");

        var stream = Assert.Single(flow.Streams, item => item.RequestType == "TicksQuery");
        Assert.Equal("TicksHandler", stream.HandlerType);
        Assert.Contains(stream.InvocationSites, site => site.Kind == "CreateStream");

        var flaky = Assert.Single(flow.Requests, request => request.RequestType == "Flaky");
        Assert.Empty(flaky.InvocationSites);
    }

    [Fact]
    public async Task Report_writer_creates_mermaid_diagram_artifacts()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "conduitr-visualizer-tests", Guid.NewGuid().ToString("N"));
        var flow = new ConduitFlow(
            Path.GetFullPath("ConduitR.sln"),
            new[]
            {
                new RequestFlow(
                    "CreateOrder",
                    "CreateOrderResult",
                    "CreateOrderHandler",
                    Array.Empty<InvocationSite>(),
                    new[] { new PipelineBehaviorInfo("ValidationBehavior<CreateOrder, CreateOrderResult>", 1, "test") },
                    Array.Empty<HandlerDependencyInfo>())
            },
            Array.Empty<NotificationFlow>(),
            Array.Empty<StreamFlow>(),
            Array.Empty<VisualizerDiagnostic>());
        var writer = new ConduitReportWriter();

        try
        {
            await writer.WriteAsync(flow, outputDirectory);

            var diagramPath = Path.Combine(outputDirectory, "diagrams", "CreateOrder.mmd");
            Assert.True(File.Exists(diagramPath));
            var diagram = await File.ReadAllTextAsync(diagramPath);
            Assert.Contains("sequenceDiagram", diagram);
            Assert.Contains("ValidationBehavior<CreateOrder, CreateOrderResult>", diagram);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void Markdown_report_embeds_mermaid_and_behavior_source_links()
    {
        var flow = new ConduitFlow(
            Path.GetFullPath("ConduitR.sln"),
            new[]
            {
                new RequestFlow(
                    "CreateOrder",
                    "CreateOrderResult",
                    "CreateOrderHandler",
                    Array.Empty<InvocationSite>(),
                    new[]
                    {
                        new PipelineBehaviorInfo(
                            "ValidationBehavior<CreateOrder, CreateOrderResult>",
                            1,
                            "test",
                            "ValidationBehavior",
                            new SourceLocation(Path.GetFullPath("ValidationBehavior.cs"), 7))
                    },
                    Array.Empty<HandlerDependencyInfo>())
            },
            Array.Empty<NotificationFlow>(),
            Array.Empty<StreamFlow>(),
            Array.Empty<VisualizerDiagnostic>());

        var markdown = MarkdownReportRenderer.Render(flow);

        Assert.Contains("class `ValidationBehavior`", markdown);
        Assert.Contains("```mermaid", markdown);
        Assert.Contains("sequenceDiagram", markdown);
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ConduitR.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the ConduitR repository root.");
    }
}
