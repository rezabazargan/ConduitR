using System.Text.RegularExpressions;

namespace ConduitR.Visualizer;

internal sealed partial class ConduitSourceScanner
{
    public ConduitScanResult Scan(IReadOnlyList<SourceFile> files)
    {
        var types = new List<TypeLocation>();
        var requests = new List<RequestRegistration>();
        var requestHandlers = new List<RequestHandlerRegistration>();
        var notifications = new List<NotificationRegistration>();
        var notificationHandlers = new List<NotificationHandlerRegistration>();
        var streams = new List<StreamRegistration>();
        var streamHandlers = new List<StreamHandlerRegistration>();
        var invocationSites = new List<InvocationSite>();
        var behaviors = new List<PipelineBehaviorInfo>();

        foreach (var file in files)
        {
            var normalizedText = Normalize(file.Text);
            ScanTypeDeclarations(file, normalizedText, types, requests, requestHandlers, notifications, notificationHandlers, streams, streamHandlers);
            ScanInvocations(file, invocationSites);
            ScanBehaviors(file, behaviors);
        }

        return new ConduitScanResult(
            types,
            requests,
            requestHandlers,
            notifications,
            notificationHandlers,
            streams,
            streamHandlers,
            invocationSites,
            behaviors);
    }

    private static void ScanTypeDeclarations(
        SourceFile file,
        string text,
        List<TypeLocation> types,
        List<RequestRegistration> requests,
        List<RequestHandlerRegistration> requestHandlers,
        List<NotificationRegistration> notifications,
        List<NotificationHandlerRegistration> notificationHandlers,
        List<StreamRegistration> streams,
        List<StreamHandlerRegistration> streamHandlers)
    {
        foreach (Match match in TypeDeclarationRegex().Matches(text))
        {
            var typeName = match.Groups["name"].Value;
            var interfaces = StripGenericConstraints(match.Groups["interfaces"].Value);
            var location = new SourceLocation(file.Path, GetLineNumber(text, match.Index));
            types.Add(new TypeLocation(typeName, location));

            foreach (Match request in RequestInterfaceRegex().Matches(interfaces))
            {
                requests.Add(new RequestRegistration(typeName, CleanTypeName(request.Groups["response"].Value), location));
            }

            if (NotificationInterfaceRegex().IsMatch(interfaces))
            {
                notifications.Add(new NotificationRegistration(typeName, location));
            }

            foreach (Match stream in StreamInterfaceRegex().Matches(interfaces))
            {
                streams.Add(new StreamRegistration(typeName, CleanTypeName(stream.Groups["response"].Value), location));
            }

            foreach (Match handler in RequestHandlerInterfaceRegex().Matches(interfaces))
            {
                requestHandlers.Add(new RequestHandlerRegistration(
                    CleanTypeName(handler.Groups["request"].Value),
                    CleanTypeName(handler.Groups["response"].Value),
                    typeName,
                    location,
                    FindConstructorDependencies(text, typeName)));
            }

            foreach (Match handler in NotificationHandlerInterfaceRegex().Matches(interfaces))
            {
                notificationHandlers.Add(new NotificationHandlerRegistration(
                    CleanTypeName(handler.Groups["notification"].Value),
                    typeName,
                    location));
            }

            foreach (Match handler in StreamHandlerInterfaceRegex().Matches(interfaces))
            {
                streamHandlers.Add(new StreamHandlerRegistration(
                    CleanTypeName(handler.Groups["request"].Value),
                    CleanTypeName(handler.Groups["response"].Value),
                    typeName,
                    location,
                    FindConstructorDependencies(text, typeName)));
            }
        }
    }

    private static void ScanInvocations(SourceFile file, List<InvocationSite> invocationSites)
    {
        var lines = file.Text.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.Contains(".Send(", StringComparison.Ordinal) ||
                line.Contains(".Publish(", StringComparison.Ordinal) ||
                line.Contains(".CreateStream(", StringComparison.Ordinal) ||
                line.Contains(".MapMediatorPost<", StringComparison.Ordinal))
            {
                invocationSites.Add(new InvocationSite(
                    DetermineInvocationKind(line),
                    file.Path,
                    i + 1,
                    line));
            }
        }
    }

    private static void ScanBehaviors(SourceFile file, List<PipelineBehaviorInfo> behaviors)
    {
        var lines = file.Text.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            var order = behaviors.Count + 1;
            if (IsMethodDeclaration(line))
            {
                continue;
            }

            var addBehavior = AddBehaviorRegex().Match(line);
            if (addBehavior.Success)
            {
                behaviors.Add(new PipelineBehaviorInfo(
                    CleanTypeName(addBehavior.Groups["behavior"].Value),
                    order,
                    $"{file.Path}:{i + 1}"));
                continue;
            }

            if (line.Contains("AddConduitValidation(", StringComparison.Ordinal))
            {
                behaviors.Add(new PipelineBehaviorInfo("ValidationBehavior<,>", order, $"{file.Path}:{i + 1}"));
            }
            else if (line.Contains("AddConduitProcessing(", StringComparison.Ordinal))
            {
                behaviors.Add(new PipelineBehaviorInfo("PreProcessingBehavior<,>", order, $"{file.Path}:{i + 1}"));
                behaviors.Add(new PipelineBehaviorInfo("PostProcessingBehavior<,>", order + 1, $"{file.Path}:{i + 1}"));
            }
            else if (line.Contains("AddConduitResiliencePolly(", StringComparison.Ordinal))
            {
                behaviors.Add(new PipelineBehaviorInfo("ResilienceBehavior<,>", order, $"{file.Path}:{i + 1}"));
            }
        }
    }

    private static IReadOnlyList<HandlerDependencyInfo> FindConstructorDependencies(string text, string typeName)
    {
        var dependencies = new List<HandlerDependencyInfo>();
        var constructorPattern = $@"public\s+{Regex.Escape(typeName)}\s*\((?<parameters>[^)]*)\)";
        var match = Regex.Match(text, constructorPattern, RegexOptions.Multiline);
        if (!match.Success) return dependencies;

        var parameters = match.Groups["parameters"].Value;
        foreach (var parameter in parameters.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = parameter.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 2)
            {
                dependencies.Add(new HandlerDependencyInfo(CleanTypeName(parts[^2]), "constructor"));
            }
        }

        return dependencies;
    }

    private static string DetermineInvocationKind(string line)
    {
        if (line.Contains(".MapMediatorPost<", StringComparison.Ordinal)) return "MapMediatorPost";
        if (line.Contains(".CreateStream(", StringComparison.Ordinal)) return "CreateStream";
        if (line.Contains(".Publish(", StringComparison.Ordinal)) return "Publish";
        return "Send";
    }

    private static string Normalize(string text) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n');

    private static string CleanTypeName(string value) =>
        value.Trim().Replace("global::", string.Empty, StringComparison.Ordinal);

    private static bool IsMethodDeclaration(string line) =>
        line.StartsWith("public ", StringComparison.Ordinal) ||
        line.StartsWith("private ", StringComparison.Ordinal) ||
        line.StartsWith("internal ", StringComparison.Ordinal) ||
        line.StartsWith("protected ", StringComparison.Ordinal);

    private static string StripGenericConstraints(string interfaces)
    {
        var constraintIndex = interfaces.IndexOf("where ", StringComparison.Ordinal);
        return constraintIndex < 0 ? interfaces : interfaces[..constraintIndex];
    }

    private static int GetLineNumber(string text, int index) =>
        text.AsSpan(0, index).Count('\n') + 1;

    [GeneratedRegex(@"public\s+(?:sealed\s+|abstract\s+|static\s+|partial\s+)*?(?:record\s+(?:class\s+)?|class\s+|struct\s+)(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:\s*<[^>]+>)?(?:\s*\([^)]*\))?\s*:\s*(?<interfaces>[^\{\;]+)", RegexOptions.Multiline)]
    private static partial Regex TypeDeclarationRegex();

    [GeneratedRegex(@"\bIRequest\s*<\s*(?<response>[^>]+)\s*>")]
    private static partial Regex RequestInterfaceRegex();

    [GeneratedRegex(@"\bINotification\b")]
    private static partial Regex NotificationInterfaceRegex();

    [GeneratedRegex(@"\bIStreamRequest\s*<\s*(?<response>[^>]+)\s*>")]
    private static partial Regex StreamInterfaceRegex();

    [GeneratedRegex(@"\bIRequestHandler\s*<\s*(?<request>[^,>]+)\s*,\s*(?<response>[^>]+)\s*>")]
    private static partial Regex RequestHandlerInterfaceRegex();

    [GeneratedRegex(@"\bINotificationHandler\s*<\s*(?<notification>[^>]+)\s*>")]
    private static partial Regex NotificationHandlerInterfaceRegex();

    [GeneratedRegex(@"\bIStreamRequestHandler\s*<\s*(?<request>[^,>]+)\s*,\s*(?<response>[^>]+)\s*>")]
    private static partial Regex StreamHandlerInterfaceRegex();

    [GeneratedRegex(@"AddBehavior\s*\(\s*typeof\s*\(\s*(?<behavior>[^)]+)\s*\)\s*\)")]
    private static partial Regex AddBehaviorRegex();
}
