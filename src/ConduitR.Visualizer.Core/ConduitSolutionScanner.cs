namespace ConduitR.Visualizer;

public sealed class ConduitSolutionScanner : IConduitSolutionScanner
{
    public async Task<ConduitFlow> ScanAsync(string targetPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);

        var fullPath = Path.GetFullPath(targetPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The solution or project file could not be found.", fullPath);
        }

        var files = await SourceFileCollector.CollectAsync(fullPath, cancellationToken).ConfigureAwait(false);
        var scan = new ConduitSourceScanner().Scan(files);
        var diagnostics = new List<VisualizerDiagnostic>
        {
            new("Info", $"Scanned {files.Count} C# source files.")
        };

        var requestFlows = BuildRequestFlows(scan, diagnostics);
        var notificationFlows = BuildNotificationFlows(scan);
        var streamFlows = BuildStreamFlows(scan, diagnostics);

        return new ConduitFlow(fullPath, requestFlows, notificationFlows, streamFlows, diagnostics);
    }

    private static IReadOnlyList<RequestFlow> BuildRequestFlows(ConduitScanResult scan, List<VisualizerDiagnostic> diagnostics)
    {
        var requestTypes = scan.Requests
            .GroupBy(request => request.RequestType)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        foreach (var handler in scan.RequestHandlers)
        {
            requestTypes.TryAdd(handler.RequestType, new RequestRegistration(handler.RequestType, handler.ResponseType, handler.Location));
        }

        var flows = new List<RequestFlow>();
        foreach (var request in requestTypes.Values.OrderBy(request => request.RequestType, StringComparer.Ordinal))
        {
            var handlers = scan.RequestHandlers
                .Where(handler => string.Equals(handler.RequestType, request.RequestType, StringComparison.Ordinal))
                .ToArray();

            if (handlers.Length == 0)
            {
                diagnostics.Add(new VisualizerDiagnostic("Warning", $"Request '{request.RequestType}' has no discovered handler."));
            }
            else if (handlers.Length > 1)
            {
                diagnostics.Add(new VisualizerDiagnostic("Error", $"Request '{request.RequestType}' has multiple discovered handlers."));
            }

            var handler = handlers.FirstOrDefault();
            flows.Add(new RequestFlow(
                request.RequestType,
                request.ResponseType,
                handler?.HandlerType,
                FindInvocationSites(scan.InvocationSites, request.RequestType, "Send", "MapMediatorPost"),
                CloseBehaviors(scan.Behaviors, scan.Types, request.RequestType, request.ResponseType),
                handler?.Dependencies ?? Array.Empty<HandlerDependencyInfo>()));
        }

        return flows;
    }

    private static IReadOnlyList<NotificationFlow> BuildNotificationFlows(ConduitScanResult scan)
    {
        var notificationTypes = scan.Notifications
            .Select(notification => notification.NotificationType)
            .Concat(scan.NotificationHandlers.Select(handler => handler.NotificationType))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);

        return notificationTypes
            .Select(notificationType => new NotificationFlow(
                notificationType,
                scan.NotificationHandlers
                    .Where(handler => string.Equals(handler.NotificationType, notificationType, StringComparison.Ordinal))
                    .Select(handler => handler.HandlerType)
                    .Order(StringComparer.Ordinal)
                    .ToArray(),
                FindInvocationSites(scan.InvocationSites, notificationType, "Publish")))
            .ToArray();
    }

    private static IReadOnlyList<StreamFlow> BuildStreamFlows(ConduitScanResult scan, List<VisualizerDiagnostic> diagnostics)
    {
        var streamTypes = scan.Streams
            .GroupBy(stream => stream.RequestType)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        foreach (var handler in scan.StreamHandlers)
        {
            streamTypes.TryAdd(handler.RequestType, new StreamRegistration(handler.RequestType, handler.ResponseType, handler.Location));
        }

        var flows = new List<StreamFlow>();
        foreach (var stream in streamTypes.Values.OrderBy(stream => stream.RequestType, StringComparer.Ordinal))
        {
            var handlers = scan.StreamHandlers
                .Where(handler => string.Equals(handler.RequestType, stream.RequestType, StringComparison.Ordinal))
                .ToArray();

            if (handlers.Length == 0)
            {
                diagnostics.Add(new VisualizerDiagnostic("Warning", $"Stream request '{stream.RequestType}' has no discovered handler."));
            }
            else if (handlers.Length > 1)
            {
                diagnostics.Add(new VisualizerDiagnostic("Error", $"Stream request '{stream.RequestType}' has multiple discovered handlers."));
            }

            var handler = handlers.FirstOrDefault();
            flows.Add(new StreamFlow(
                stream.RequestType,
                stream.ResponseType,
                handler?.HandlerType,
                FindInvocationSites(scan.InvocationSites, stream.RequestType, "CreateStream"),
                CloseBehaviors(scan.Behaviors, scan.Types, stream.RequestType, stream.ResponseType),
                handler?.Dependencies ?? Array.Empty<HandlerDependencyInfo>()));
        }

        return flows;
    }

    private static IReadOnlyList<InvocationSite> FindInvocationSites(
        IReadOnlyList<InvocationSite> invocationSites,
        string typeName,
        params string[] kinds)
    {
        return invocationSites
            .Where(site => kinds.Contains(site.Kind, StringComparer.Ordinal) &&
                site.Expression.Contains(typeName, StringComparison.Ordinal))
            .ToArray();
    }

    private static IReadOnlyList<PipelineBehaviorInfo> CloseBehaviors(
        IReadOnlyList<PipelineBehaviorInfo> behaviors,
        IReadOnlyList<TypeLocation> types,
        string requestType,
        string responseType)
    {
        return behaviors
            .OrderBy(behavior => behavior.Order)
            .Select((behavior, index) =>
            {
                var className = GetOpenGenericTypeName(behavior.BehaviorType);
                var classLocation = types.FirstOrDefault(type =>
                    string.Equals(type.TypeName, className, StringComparison.Ordinal))?.Location;

                return behavior with
                {
                    Order = index + 1,
                    BehaviorType = behavior.BehaviorType.Replace("<,>", $"<{requestType}, {responseType}>", StringComparison.Ordinal),
                    ClassName = className,
                    ClassLocation = classLocation
                };
            })
            .ToArray();
    }

    private static string GetOpenGenericTypeName(string typeName)
    {
        var genericStart = typeName.IndexOf('<', StringComparison.Ordinal);
        return genericStart < 0 ? typeName : typeName[..genericStart];
    }
}
