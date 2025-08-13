# ConduitR

> A lean, high‑performance mediator-style messaging library for .NET — request/response, commands, queries, notifications, and pipeline behaviors — designed as a modern, open alternative to MediatR.

[![Build](https://img.shields.io/github/actions/workflow/status/rezabazargan/conduitr/build.yml?branch=main)](../../actions)
[![NuGet](https://img.shields.io/nuget/v/ConduitR.svg)](https://www.nuget.org/packages/ConduitR)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ConduitR.svg)](https://www.nuget.org/packages/ConduitR)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## Why ConduitR?

* **Drop‑in familiar**: Request/Response and Publish/Subscribe abstractions you already know.
* **Pipeline behaviors**: Cross‑cutting concerns (logging, validation, caching, retries) via simple middleware.
* **DI‑friendly**: First‑class support for `Microsoft.Extensions.DependencyInjection` (works with others too).
* **Fast & allocation‑aware**: Minimal overhead, value‑task first, pooling where appropriate.
* **Agnostic & composable**: No framework lock‑in. Use in Console, Web, Worker, Functions.
* **Docs & samples**: Clear getting‑started, API docs (DocFX), and real‑world examples.

> ✳️ This repository is scaffolded to be **adoptable for any .NET library**, not just a mediator. Swap the sample code and keep the structure.

---

## Table of Contents

* [Packages](#packages)
* [Quick Start](#quick-start)
* [Samples](#samples)
* [Project Layout](#project-layout)
* [Design Goals](#design-goals)
* [Documentation](#documentation)
* [Development](#development)
* [Contributing](#contributing)
* [Versioning & Releases](#versioning--releases)
* [Security](#security)
* [License](#license)
* [Acknowledgements](#acknowledgements)

---

## Packages

This repo can produce multiple NuGet packages. Replace or remove what you don’t need.

* `ConduitR.Abstractions` — Interfaces & contracts (requests, handlers, behaviors).
* `ConduitR` — Core implementation.
* `ConduitR.DependencyInjection` — Extensions for `IServiceCollection`.
* `ConduitR.AspNetCore` *(optional)* — Helpers for ASP.NET Core (model binding, filters).
* `ConduitR.SourceGenerators` *(optional)* — Performance‑oriented generators.

> NuGet IDs are set via `Directory.Build.props`. Change once, propagate everywhere.

---

## Quick Start

### 1) Install (pick what you need)

```powershell
dotnet add package ConduitR.Abstractions
dotnet add package ConduitR
dotnet add package ConduitR.DependencyInjection
```

### 2) Define a Request & Handler

```csharp
// in YourProject/Application/GetTime.cs
using System.Threading;
using System.Threading.Tasks;
using ConduitR.Abstractions;

public sealed record GetTimeQuery(string TimeZoneId) : IRequest<string>;

public sealed class GetTimeHandler : IRequestHandler<GetTimeQuery, string>
{
    public ValueTask<string> Handle(GetTimeQuery request, CancellationToken ct)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId);
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
        return ValueTask.FromResult(now.ToString("O"));
    }
}
```

### 3) Register & Use

```csharp
// Program.cs
using ConduitR.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConduit(cfg =>
{
    cfg.AddHandlersFromAssemblies(typeof(GetTimeHandler).Assembly);
    cfg.AddBehavior(typeof(LoggingBehavior<,>)); // example behavior
});

var app = builder.Build();

app.MapGet("/time/{tz}", async (string tz, IMediator mediator, CancellationToken ct)
    => await mediator.Send(new GetTimeQuery(tz), ct));

app.Run();
```

### Notifications (Pub/Sub)

```csharp
public sealed record UserRegistered(Guid Id, string Email) : INotification;

public sealed class SendWelcomeEmail : INotificationHandler<UserRegistered>
{
    public Task Handle(UserRegistered notification, CancellationToken ct)
    {
        // send email
        return Task.CompletedTask;
    }
}

// somewhere in your app
await mediator.Publish(new UserRegistered(user.Id, user.Email), ct);
```

---

## Samples

* **Samples.Console** — simplest possible usage
* **Samples.WebApi** — minimal API with DI, logging behavior, notifications

Run a sample:

```bash
cd samples/Samples.WebApi
dotnet run
```

---

## Project Layout

```
{REPO_ROOT}/
├─ src/
│  ├─ ConduitR.Abstractions/
│  ├─ ConduitR/
│  ├─ ConduitR.DependencyInjection/
│  ├─ ConduitR.AspNetCore/                 # optional
│  ├─ ConduitR.SourceGenerators/           # optional
│  ├─ Directory.Build.props
│  └─ Directory.Build.targets
├─ tests/
│  ├─ ConduitR.Tests/                     # unit tests
│  └─ ConduitR.IntegrationTests/          # integration tests
├─ benchmarks/
│  └─ ConduitR.Benchmarks/                # BenchmarkDotNet
├─ samples/
│  ├─ Samples.Console/
│  └─ Samples.WebApi/
├─ docs/
│  ├─ getting-started.md
│  ├─ architecture.md
│  ├─ behaviors.md
│  ├─ roadmap.md
│  ├─ faq.md
│  └─ changelog.md
├─ .github/
│  ├─ workflows/
│  │  └─ build.yml
│  ├─ ISSUE_TEMPLATE/
│  │  ├─ bug_report.md
│  │  └─ feature_request.md
│  ├─ pull_request_template.md
│  ├─ CODEOWNERS
│  └─ FUNDING.yml
├─ .editorconfig
├─ .gitattributes
├─ LICENSE
├─ README.md
└─ CONTRIBUTING.md
```

Key repository features:

* Centralized versioning & metadata in `Directory.Build.props`.
* SourceLink + deterministic builds for great debugging.
* Nullable enabled, analyzers (StyleCop/IDEs) tuned in `Directory.Build.props`.
* GitHub Actions: CI (build/test/pack), PR validation, NuGet publish on tag.
* DocFX documentation site (optional) — HTML output deployable to GitHub Pages.

---

## Design Goals

* **Minimal API surface** with strong conventions.
* **Predictable performance**: lean abstractions, ValueTask‑first where reasonable.
* **Cancellation propagation** everywhere.
* **OpenTelemetry‑friendly** diagnostics hooks.
* **Extensible pipeline**: behaviors chain with short‑circuiting.

---

## Documentation

* `docs/getting-started.md` — extended quickstart
* `docs/architecture.md` — internals & design tradeoffs
* `docs/behaviors.md` — authoring pipeline behaviors
* `docs/roadmap.md` — planned features
* `docs/faq.md` — common questions
* `docs/changelog.md` — keep a human‑readable history (also use GitHub Releases)

> API docs are generated with **DocFX** from XML comments. See `docs/` for instructions.

---

## Development

### Prerequisites

* .NET 8 or 9 SDK
* (optional) VS Code or Visual Studio 2022+

### Build, Test, Pack

```bash
dotnet build
dotnet test --configuration Release
dotnet pack -p:PackageVersion=0.1.0 -c Release -o ./artifacts
```

### Running Benchmarks

```bash
cd benchmarks/ConduitR.Benchmarks
dotnet run -c Release
```

### Local API Docs (DocFX)

```bash
# once
dotnet tool update -g docfx
# generate site
cd docs
docfx
docfx serve _site
```

---

## Contributing

Contributions are welcome! Please read `CONTRIBUTING.md` for how to:

* Propose features and file issues
* Run the full build + tests locally
* Submit a PR (formatting, analyzers, commit conventions)

We follow **Conventional Commits** and **Semantic Versioning**.

---

## Versioning & Releases

* **SemVer**: `MAJOR.MINOR.PATCH`
* CI publishes prerelease packages on every push to `main` using `-pre{shortsha}`.
* Create a Git tag `vX.Y.Z` on `main` to publish a stable NuGet release and generate GitHub Release notes.

> The CI workflow is in `.github/workflows/build.yml`. Update secrets `NUGET_API_KEY` as needed.

---

## Security

Please do **not** open public issues for security problems. Email `{SECURITY_CONTACT_EMAIL}` with details.

---

## License

This project is licensed under the **MIT License** — see `LICENSE`.

---

## Acknowledgements

Inspired by the excellent ideas behind **CQRS** and existing mediator libraries.


## FAQ (short)

**Q: Is this a drop‑in replacement for MediatR?**
A: The concepts, interfaces, and behaviors are familiar. Small API differences are intentional for performance and clarity. A thin shim package can be provided if desired.

**Q: Can I use other DI containers?**
A: Yes. The core is container‑agnostic; `DependencyInjection` is for convenience.

**Q: Does it support streaming?**
A: Yes via `IStreamRequest<T>`/`IStreamRequestHandler<TRequest,T>` (optional package or sample provided).
