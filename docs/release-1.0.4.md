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