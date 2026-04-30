# ConduitR.Visualizer.Core

Reusable static-analysis engine for ConduitR Visualizer tooling.

`ConduitR.Visualizer.Core` scans C# solutions and projects to build a structured model of ConduitR mediator flows. It is the shared engine behind the Visualizer CLI and future tooling integrations.

## Install

```bash
dotnet add package ConduitR.Visualizer.Core --version 1.0.5
```

For prerelease builds:

```bash
dotnet add package ConduitR.Visualizer.Core --prerelease
```

## What It Provides

- solution/project source collection
- request flow discovery
- notification flow discovery
- stream flow discovery
- handler lookup and duplicate detection
- pipeline behavior inference
- handler dependency discovery
- Markdown, JSON, and Mermaid report generation

## Basic Usage

```csharp
using ConduitR.Visualizer;

IConduitSolutionScanner scanner = new ConduitSolutionScanner();
IConduitReportWriter writer = new ConduitReportWriter();

var result = await scanner.ScanAsync("./MyApp.sln", cancellationToken);
await writer.WriteAsync(result, "./artifacts/conduitr", cancellationToken);
```

Generated files:

```text
artifacts/conduitr/
  flows.md
  flows.json
  diagrams/
    *.mmd
```

## Flow Model

The scanner returns a `ConduitScanResult` with:

- `RequestFlow` entries for `IRequest<TResponse>`
- `NotificationFlow` entries for `INotification`
- `StreamFlow` entries for `IStreamRequest<TResponse>`
- `VisualizerDiagnostic` entries for missing or ambiguous wiring

Each flow keeps source locations so generated reports can link back to the code that declared or invoked the flow.

## Static Analysis Limits

The core package does not execute your application or build a runtime service provider. It reads source code and infers ConduitR wiring from known patterns. Dynamic assembly lists, reflection-heavy registration, and custom wrapper methods may need future hints or conventions.

## Related Packages

- `ConduitR.Visualizer.Cli`: .NET tool that uses this engine
- `ConduitR.Visualizer.Analyzers`: Visual Studio/Roslyn design-time handler hints
- `ConduitR.DependencyInjection`: runtime registration helpers detected by the scanner

## More Documentation

- [Repository README](https://github.com/rezabazargan/ConduitR)
- [Release 1.0.5 plan](https://github.com/rezabazargan/ConduitR/blob/main/docs/release-1.0.5.md)
