namespace ConduitR.Visualizer;

internal sealed record TypeLocation(
    string TypeName,
    SourceLocation Location);

internal sealed record RequestRegistration(string RequestType, string ResponseType, SourceLocation Location);

internal sealed record RequestHandlerRegistration(
    string RequestType,
    string ResponseType,
    string HandlerType,
    SourceLocation Location,
    IReadOnlyList<HandlerDependencyInfo> Dependencies);

internal sealed record NotificationRegistration(string NotificationType, SourceLocation Location);

internal sealed record NotificationHandlerRegistration(string NotificationType, string HandlerType, SourceLocation Location);

internal sealed record StreamRegistration(string RequestType, string ResponseType, SourceLocation Location);

internal sealed record StreamHandlerRegistration(
    string RequestType,
    string ResponseType,
    string HandlerType,
    SourceLocation Location,
    IReadOnlyList<HandlerDependencyInfo> Dependencies);
