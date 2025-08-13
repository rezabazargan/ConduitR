using System.Diagnostics;

namespace ConduitR;

/// <summary>Holds the ActivitySource for ConduitR so apps can subscribe via OpenTelemetry.</summary>
public static class ConduitRTelemetry
{
    public const string ActivitySourceName = "ConduitR";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
