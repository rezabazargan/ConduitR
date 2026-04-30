# ConduitR.Visualizer.Analyzers

Visual Studio and Roslyn design-time hints for ConduitR mediator calls.

`ConduitR.Visualizer.Analyzers` helps answer the question every mediator user eventually asks: "I see `mediator.Send(...)`, but where is the handler?"

## Install

```bash
dotnet add package ConduitR.Visualizer.Analyzers --version 1.0.5
```

For prerelease builds:

```bash
dotnet add package ConduitR.Visualizer.Analyzers --prerelease
```

Recommended project reference:

```xml
<PackageReference Include="ConduitR.Visualizer.Analyzers" Version="1.0.5" PrivateAssets="all" />
```

`PrivateAssets="all"` keeps the analyzer as a development-time dependency. It does not flow transitively to consumers of your library.

## Visual Studio Experience

When the cursor is on a ConduitR call, Visual Studio shows the resolved handler as an informational diagnostic.

![ConduitR Send handler hint](https://raw.githubusercontent.com/rezabazargan/ConduitR/main/docs/images/Send.png)

Streaming calls are supported too:

![ConduitR CreateStream handler hint](https://raw.githubusercontent.com/rezabazargan/ConduitR/main/docs/images/CreateStream.png)

The analyzer also registers a lightbulb action:

```text
Go to ConduitR handler 'GetTimeHandler'
```

Use `Show potential fixes` or `Ctrl+.` from the ConduitR info message to navigate to the handler document.

## Supported Calls

- `mediator.Send(new SomeRequest(...))`
- `mediator.CreateStream(new SomeStreamRequest(...))`

The analyzer resolves:

- request type
- stream request type
- handler class
- handler source file and line

## Diagnostic

| ID | Severity | Meaning |
|---|---|---|
| `CDRVS001` | Info | A ConduitR request or stream request handler was resolved |

Example message:

```text
CDRVS001: ConduitR request 'GetTimeQuery' is handled by 'GetTimeHandler' at Program.cs:57
```

## Why It Is An Analyzer Package

This package does not require a Visual Studio extension. It uses standard Roslyn analyzer and code-fix APIs, so adding the NuGet package is enough for supported IDE hosts to load it.

## Related Packages

- `ConduitR.Visualizer.Cli`: generates Markdown, JSON, and Mermaid architecture reports
- `ConduitR.Visualizer.Core`: shared static-analysis and report model
- `ConduitR`: core mediator runtime

## More Documentation

- [Repository README](https://github.com/rezabazargan/ConduitR)
- [Release 1.0.5 plan](https://github.com/rezabazargan/ConduitR/blob/main/docs/release-1.0.5.md)
