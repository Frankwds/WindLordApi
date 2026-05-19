---
description: Testing conventions for WindLordApi unit and integration coverage
applyTo: "src/WindLordApi.Tests/**/*"
---

# Testing Conventions

## Test Framework

Use xUnit v3 with FluentAssertions for assertions, Moq for mocks, and Testcontainers PostgreSQL for integration coverage.

## Test Organization

Tests are organized by scope and concern.

```text
src/WindLordApi.Tests/
  Unit/
    Repositories/
    Services/
  Integration/
    Repositories/
    Services/
  Helpers/
```

## Test Naming

- Use `*Tests.cs` for unit suites.
- Use `*IntegrationTests.cs` when the suite exercises the database or other integrated behavior.
- Name tests by behavior, not by private method.

## Test Strategy

- Map OpenSpec scenarios directly into tests.
- Use unit tests for service logic, mapping rules, and validation behavior.
- Use integration tests for repository, migration, and persistence behavior.
- Extend shared builders when a new domain shape is needed repeatedly.

## Validation Commands

- `dotnet build WindLordApi.sln`
- `dotnet test src/WindLordApi.Tests/WindLordApi.Tests.csproj`

## Anti-Patterns To Avoid

- Do not rely only on happy-path tests for sync workflows.
- Do not bypass shared builders with large inline object graphs when the pattern already exists.
- Do not update behavior specs without corresponding test updates.