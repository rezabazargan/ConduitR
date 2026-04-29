using System.Text;

namespace ConduitR.Visualizer;

public static class MarkdownReportRenderer
{
    public static string Render(ConduitFlow flow)
    {
        ArgumentNullException.ThrowIfNull(flow);

        var builder = new StringBuilder();
        builder.AppendLine("# ConduitR Flow Report");
        builder.AppendLine();
        builder.AppendLine($"Target: `{flow.TargetPath}`");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"- Requests: {flow.Requests.Count}");
        builder.AppendLine($"- Notifications: {flow.Notifications.Count}");
        builder.AppendLine($"- Streams: {flow.Streams.Count}");
        builder.AppendLine($"- Diagnostics: {flow.Diagnostics.Count}");
        builder.AppendLine();

        AppendDiagnostics(builder, flow.Diagnostics);
        AppendRequests(builder, flow.Requests);
        AppendNotifications(builder, flow.Notifications);
        AppendStreams(builder, flow.Streams);

        return builder.ToString();
    }

    private static void AppendDiagnostics(StringBuilder builder, IReadOnlyList<VisualizerDiagnostic> diagnostics)
    {
        builder.AppendLine("## Diagnostics");
        builder.AppendLine();

        if (diagnostics.Count == 0)
        {
            builder.AppendLine("No diagnostics.");
            builder.AppendLine();
            return;
        }

        foreach (var diagnostic in diagnostics)
        {
            builder.Append("- ");
            builder.Append('[');
            builder.Append(diagnostic.Severity);
            builder.Append("] ");
            builder.Append(diagnostic.Message);
            if (!string.IsNullOrWhiteSpace(diagnostic.FilePath))
            {
                builder.Append(" (`");
                builder.Append(diagnostic.FilePath);
                if (diagnostic.Line is not null)
                {
                    builder.Append(':');
                    builder.Append(diagnostic.Line.Value);
                }
                builder.Append("`)");
            }
            builder.AppendLine();
        }

        builder.AppendLine();
    }

    private static void AppendRequests(StringBuilder builder, IReadOnlyList<RequestFlow> requests)
    {
        builder.AppendLine("## Request Flows");
        builder.AppendLine();

        if (requests.Count == 0)
        {
            builder.AppendLine("No request flows discovered yet.");
            builder.AppendLine();
            return;
        }

        foreach (var request in requests)
        {
            builder.AppendLine($"### {request.RequestType}");
            builder.AppendLine();
            builder.AppendLine($"- Response: `{request.ResponseType}`");
            builder.AppendLine($"- Handler: `{request.HandlerType ?? "not discovered"}`");
            AppendInvocationSites(builder, request.InvocationSites);
            AppendPipeline(builder, request.Pipeline);
            AppendDependencies(builder, request.Dependencies);
            AppendMermaidDiagram(builder, request.RequestType, RenderRequestDiagram(request));
            builder.AppendLine();
        }
    }

    private static void AppendNotifications(StringBuilder builder, IReadOnlyList<NotificationFlow> notifications)
    {
        builder.AppendLine("## Notification Flows");
        builder.AppendLine();

        if (notifications.Count == 0)
        {
            builder.AppendLine("No notification flows discovered yet.");
            builder.AppendLine();
            return;
        }

        foreach (var notification in notifications)
        {
            builder.AppendLine($"### {notification.NotificationType}");
            builder.AppendLine();
            if (notification.HandlerTypes.Count == 0)
            {
                builder.AppendLine("- Handlers: `not discovered`");
            }
            else
            {
                builder.AppendLine("- Handlers:");
                foreach (var handler in notification.HandlerTypes)
                {
                    builder.AppendLine($"  - `{handler}`");
                }
            }
            AppendInvocationSites(builder, notification.InvocationSites);
            AppendMermaidDiagram(builder, notification.NotificationType, RenderNotificationDiagram(notification));
            builder.AppendLine();
        }
    }

    private static void AppendStreams(StringBuilder builder, IReadOnlyList<StreamFlow> streams)
    {
        builder.AppendLine("## Stream Flows");
        builder.AppendLine();

        if (streams.Count == 0)
        {
            builder.AppendLine("No stream flows discovered yet.");
            builder.AppendLine();
            return;
        }

        foreach (var stream in streams)
        {
            builder.AppendLine($"### {stream.RequestType}");
            builder.AppendLine();
            builder.AppendLine($"- Response: `{stream.ResponseType}`");
            builder.AppendLine($"- Handler: `{stream.HandlerType ?? "not discovered"}`");
            AppendInvocationSites(builder, stream.InvocationSites);
            AppendPipeline(builder, stream.Pipeline);
            AppendDependencies(builder, stream.Dependencies);
            AppendMermaidDiagram(builder, stream.RequestType, RenderStreamDiagram(stream));
            builder.AppendLine();
        }
    }

    private static void AppendInvocationSites(StringBuilder builder, IReadOnlyList<InvocationSite> invocationSites)
    {
        if (invocationSites.Count == 0)
        {
            builder.AppendLine("- Invocation sites: `not discovered`");
            return;
        }

        builder.AppendLine("- Invocation sites:");
        foreach (var site in invocationSites)
        {
            builder.AppendLine($"  - `{site.Kind}` at {FormatSourceLink(site.FilePath, site.Line)}");
            builder.AppendLine($"    - `{site.Expression}`");
        }
    }

    private static void AppendPipeline(StringBuilder builder, IReadOnlyList<PipelineBehaviorInfo> pipeline)
    {
        if (pipeline.Count == 0)
        {
            builder.AppendLine("- Pipeline: `none discovered`");
            return;
        }

        builder.AppendLine("- Pipeline:");
        foreach (var behavior in pipeline.OrderBy(behavior => behavior.Order))
        {
            builder.Append($"  - {behavior.Order}. `{behavior.BehaviorType}`");
            if (!string.IsNullOrWhiteSpace(behavior.ClassName))
            {
                builder.Append($" class `{behavior.ClassName}`");
            }

            if (behavior.ClassLocation is not null)
            {
                builder.Append($" at {FormatSourceLink(behavior.ClassLocation.FilePath, behavior.ClassLocation.Line)}");
            }
            else
            {
                builder.Append($" registered at `{behavior.Source}`");
            }

            builder.AppendLine();
        }
    }

    private static void AppendDependencies(StringBuilder builder, IReadOnlyList<HandlerDependencyInfo> dependencies)
    {
        if (dependencies.Count == 0)
        {
            builder.AppendLine("- Handler dependencies: `none discovered`");
            return;
        }

        builder.AppendLine("- Handler dependencies:");
        foreach (var dependency in dependencies)
        {
            builder.AppendLine($"  - `{dependency.Type}` via {dependency.Source}");
        }
    }

    private static void AppendMermaidDiagram(StringBuilder builder, string title, string diagram)
    {
        builder.AppendLine("- Diagram:");
        builder.AppendLine();
        builder.AppendLine("```mermaid");
        builder.Append(diagram);
        builder.AppendLine("```");
    }

    private static string RenderRequestDiagram(RequestFlow request)
    {
        var builder = CreateDiagramHeader(request.RequestType);
        builder.AppendLine($"    Caller->>Mediator: Send({request.RequestType})");
        AppendPipelineDiagram(builder, request.Pipeline);
        builder.AppendLine($"    Mediator->>Handler: {request.HandlerType ?? "handler not discovered"}");
        builder.AppendLine($"    Handler-->>Mediator: {request.ResponseType}");
        builder.AppendLine($"    Mediator-->>Caller: {request.ResponseType}");
        return builder.ToString();
    }

    private static string RenderStreamDiagram(StreamFlow stream)
    {
        var builder = CreateDiagramHeader(stream.RequestType);
        builder.AppendLine($"    Caller->>Mediator: CreateStream({stream.RequestType})");
        AppendPipelineDiagram(builder, stream.Pipeline);
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

    private static void AppendPipelineDiagram(StringBuilder builder, IReadOnlyList<PipelineBehaviorInfo> pipeline)
    {
        foreach (var behavior in pipeline.OrderBy(behavior => behavior.Order))
        {
            builder.AppendLine($"    Mediator->>Behavior: {behavior.BehaviorType}");
            builder.AppendLine("    Behavior-->>Mediator: next");
        }
    }

    private static string FormatSourceLink(string filePath, int line)
    {
        var normalizedPath = filePath.Replace('\\', '/');
        var label = $"{Path.GetFileName(filePath)}:{line}";
        return $"[{label}]({normalizedPath}#L{line})";
    }
}
