using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace ConduitR.Visualizer;

public sealed class ConduitReportWriter : IConduitReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task WriteAsync(ConduitFlow flow, string outputDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(flow);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        Directory.CreateDirectory(outputDirectory);
        var diagramsDirectory = Path.Combine(outputDirectory, "diagrams");
        Directory.CreateDirectory(diagramsDirectory);
        foreach (var diagramPath in Directory.EnumerateFiles(diagramsDirectory, "*.mmd"))
        {
            File.Delete(diagramPath);
        }

        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "flows.md"),
            MarkdownReportRenderer.Render(flow),
            cancellationToken).ConfigureAwait(false);

        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "flows.json"),
            JsonSerializer.Serialize(flow, JsonOptions),
            cancellationToken).ConfigureAwait(false);

        foreach (var request in flow.Requests)
        {
            await File.WriteAllTextAsync(
                Path.Combine(diagramsDirectory, $"{ToFileName(request.RequestType)}.mmd"),
                RenderRequestDiagram(request),
                cancellationToken).ConfigureAwait(false);
        }

        foreach (var stream in flow.Streams)
        {
            await File.WriteAllTextAsync(
                Path.Combine(diagramsDirectory, $"{ToFileName(stream.RequestType)}.mmd"),
                RenderStreamDiagram(stream),
                cancellationToken).ConfigureAwait(false);
        }

        foreach (var notification in flow.Notifications)
        {
            await File.WriteAllTextAsync(
                Path.Combine(diagramsDirectory, $"{ToFileName(notification.NotificationType)}.mmd"),
                RenderNotificationDiagram(notification),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private static string RenderRequestDiagram(RequestFlow request)
    {
        var builder = CreateDiagramHeader(request.RequestType);
        builder.AppendLine($"    Caller->>Mediator: Send({request.RequestType})");
        AppendPipeline(builder, request.Pipeline);
        builder.AppendLine($"    Mediator->>Handler: {request.HandlerType ?? "handler not discovered"}");
        builder.AppendLine($"    Handler-->>Mediator: {request.ResponseType}");
        builder.AppendLine($"    Mediator-->>Caller: {request.ResponseType}");
        return builder.ToString();
    }

    private static string RenderStreamDiagram(StreamFlow stream)
    {
        var builder = CreateDiagramHeader(stream.RequestType);
        builder.AppendLine($"    Caller->>Mediator: CreateStream({stream.RequestType})");
        AppendPipeline(builder, stream.Pipeline);
        builder.AppendLine($"    Mediator->>Handler: {stream.HandlerType ?? "handler not discovered"}");
        builder.AppendLine($"    Handler-->>Mediator: IAsyncEnumerable<{stream.ResponseType}>");
        builder.AppendLine($"    Mediator-->>Caller: IAsyncEnumerable<{stream.ResponseType}>");
        return builder.ToString();
    }

    private static string RenderNotificationDiagram(NotificationFlow notification)
    {
        var builder = CreateDiagramHeader(notification.NotificationType);
        builder.AppendLine($"    Caller->>Mediator: Publish({notification.NotificationType})");

        if (notification.HandlerTypes.Count == 0)
        {
            builder.AppendLine("    Mediator-->>Caller: handler not discovered");
            return builder.ToString();
        }

        foreach (var handler in notification.HandlerTypes)
        {
            builder.AppendLine($"    Mediator->>Handler: {handler}");
        }

        builder.AppendLine("    Mediator-->>Caller: complete");
        return builder.ToString();
    }

    private static StringBuilder CreateDiagramHeader(string title)
    {
        var builder = new StringBuilder();
        builder.AppendLine("sequenceDiagram");
        builder.AppendLine($"    title {title}");
        builder.AppendLine("    participant Caller");
        builder.AppendLine("    participant Mediator");
        builder.AppendLine("    participant Behavior");
        builder.AppendLine("    participant Handler");
        return builder;
    }

    private static void AppendPipeline(StringBuilder builder, IReadOnlyList<PipelineBehaviorInfo> pipeline)
    {
        foreach (var behavior in pipeline.OrderBy(behavior => behavior.Order))
        {
            builder.AppendLine($"    Mediator->>Behavior: {behavior.BehaviorType}");
            builder.AppendLine("    Behavior-->>Mediator: next");
        }
    }

    private static string ToFileName(string value)
    {
        var fileName = Regex.Replace(value, @"[^A-Za-z0-9_.-]+", "-", RegexOptions.CultureInvariant);
        return fileName.Trim('-');
    }
}
