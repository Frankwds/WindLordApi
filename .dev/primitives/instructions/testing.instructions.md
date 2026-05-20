---
description: Testing expectations for WindLordApi unit and integration changes
applyTo: "src/WindLordApi.Tests/**/*"
---

# Testing

## Test Stack
- xUnit v3 is the primary test framework.
- FluentAssertions provides assertions.
- Moq is used for isolated dependency testing.
- PostgreSQL Testcontainers are used for integration tests.
- EF Core InMemory is acceptable for narrow unit-level persistence helpers where provider semantics are not under test.

## Test Organization
- Keep unit tests in `src/WindLordApi.Tests/Unit` and integration tests in `src/WindLordApi.Tests/Integration`.
- Reuse shared helpers from `src/WindLordApi.Tests/Helpers` for container and database setup.

## Expectations
- Every behavior change should map to at least one focused validation step.
- Prefer integration tests when behavior depends on EF Core mappings, constraints, views, or PostgreSQL behavior.
- Prefer unit tests for mappings, service logic, and scheduler-adjacent decisions.

## OpenSpec Mapping
- Mirror `Given/When/Then` scenarios from `openspec/specs/` in test names and assertions when practical.
- If a spec scenario is hard to validate, call that out rather than silently skipping coverage.

## Validation Commands
- `dotnet test src/WindLordApi.Tests/WindLordApi.Tests.csproj`
- `dotnet build WindLordApi.sln`
- `openspec validate --specs`