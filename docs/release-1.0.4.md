# ConduitR v1.0.4 Release Notes

## Overview
This release includes mediator correctness fixes, telemetry accuracy improvements, CI hardening, dependency cleanup, documentation polish, and expanded unit test coverage for the ConduitR mediator library.

## Changes Since v1.0.3

### Bug Fixes
- **Sequential Publish Aggregation**: Fixed `PublishStrategy.Sequential` to properly aggregate exceptions from all handlers instead of stopping on the first exception. Now throws `AggregateException` containing all handler errors.
- **Telemetry Span Lifetime**: Fixed `Mediator.Send`, `Mediator.Publish`, and `Mediator.Stream` activities so spans stay open until the async handler, publish operation, or stream enumeration actually completes.
- **Telemetry Error Status**: Added exception events and error status tagging when send, publish, or stream operations fail.
- **Publish Handler Resolution**: Fixed the cached publish invoker to resolve notification handlers through the same service shape as the mediator, preserving direct resolver and DI behavior.
- **Publish Enumeration Efficiency**: Reworked sequential publish fallback handling to enumerate handlers once instead of repeatedly calling LINQ operations such as `Count()`, `First()`, `Skip()`, and `ElementAt()`.
- **README Rendering**: Fixed README encoding artifacts and corrected the malformed install code fence.

### Enhancements
- **Expanded Unit Tests**: Added comprehensive unit test coverage for:
  - Dependency injection extensions (`AddConduit`)
  - Mediator core functionality (send, publish, stream)
  - Pipeline behaviors (request/response, streaming)
  - ASP.NET Core integrations (middleware, extensions)
  - Resilience policies (Polly integration)
  - Validation behaviors (FluentValidation)
  - Publish strategies (sequential, parallel, stop-on-first-exception)
- **Telemetry Regression Tests**: Added tests proving send, publish, and stream activities remain active until their async work completes.
- **Version Documentation**: Updated README version references to 1.0.4.

### Build Fixes
- Updated C# language version to 'latest' for modern syntax support (global using, file-scoped namespaces)
- Removed netstandard2.0 target framework to avoid compatibility issues with newer APIs
- Removed redundant compatibility package references that are no longer needed for the current target frameworks:
  - System.Threading.Tasks.Extensions
  - Microsoft.Bcl.AsyncInterfaces
- Added System.Diagnostics.DiagnosticSource 8.0.0 for telemetry support
- Added fallback implementation for IsExternalInit on netstandard builds
- Updated target frameworks for AspNetCore project to net8.0 and net10.0 only
- Updated GitHub Actions to install both .NET 8 and .NET 10 SDKs so CI matches the solution target frameworks.
- Enabled warnings-as-errors for the solution while suppressing XML documentation warning noise.

### Infrastructure
- Updated test project references to include ASP.NET Core packages for middleware testing
- Improved test isolation and error handling
- Added generated local artifacts to `.gitignore`, including `build.log`, `BenchmarkDotNet.Artifacts/`, and `.dotnet-home/`.
- Fixed dependency injection service resolution nullability warnings.

## Breaking Changes
None.

## Known Issues
- XML documentation warning checks remain suppressed for this release while public API documentation is expanded separately.

## Testing
All 31 unit tests pass successfully.

Verified with:
```
dotnet test ConduitR.sln -c Release --verbosity minimal
```

## Installation
To install:
```
dotnet add package ConduitR --version 1.0.4
```

## Contributors
- Reza (unit test expansion and bug fixes)

---

*Release Date: April 25, 2026*
