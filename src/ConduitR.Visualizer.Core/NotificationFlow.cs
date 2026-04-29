namespace ConduitR.Visualizer;

public sealed record NotificationFlow(
    string NotificationType,
    IReadOnlyList<string> HandlerTypes,
    IReadOnlyList<InvocationSite> InvocationSites);
