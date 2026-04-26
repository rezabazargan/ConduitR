# ConduitR v1.0.4 Prerelease Notes

## Overview
This prerelease includes bug fixes and expanded unit test coverage for the ConduitR mediator library.

## Changes Since v1.0.3

### Bug Fixes
- **Sequential Publish Aggregation**: Fixed `PublishStrategy.Sequential` to properly aggregate exceptions from all handlers instead of stopping on the first exception. Now throws `AggregateException` containing all handler errors.

### Enhancements
- **Expanded Unit Tests**: Added comprehensive unit test coverage for:
  - Dependency injection extensions (`AddConduit`)
  - Mediator core functionality (send, publish, stream)
  - Pipeline behaviors (request/response, streaming)
  - ASP.NET Core integrations (middleware, extensions)
  - Resilience policies (Polly integration)
  - Validation behaviors (FluentValidation)
  - Publish strategies (sequential, parallel, stop-on-first-exception)

### Build Fixes
- Updated C# language version to 'latest' for modern syntax support (global using, file-scoped namespaces)
- Removed netstandard2.0 target framework to avoid compatibility issues with newer APIs
- Added compatibility packages for netstandard2.1:
  - System.Threading.Tasks.Extensions 4.6.0
  - Microsoft.Bcl.AsyncInterfaces 6.0.0
- Added System.Diagnostics.DiagnosticSource 8.0.0 for telemetry support
- Added fallback implementation for IsExternalInit on netstandard builds
- Updated target frameworks for AspNetCore project to net8.0 and net10.0 only

### Infrastructure
- Updated test project references to include ASP.NET Core packages for middleware testing
- Improved test isolation and error handling

## Breaking Changes
None.

## Known Issues
- XML documentation warnings in test files (non-blocking)

## Testing
All 28 unit tests pass successfully.

## Installation
This is a prerelease. To install:
```
dotnet add package ConduitR --version 1.0.4-prerelease
```

## Contributors
- Reza (unit test expansion and bug fixes)

---

*Release Date: April 25, 2026*