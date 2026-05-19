---
paths:
  - "src/WindLordApi.Tests/**/*"
---

# Testing Conventions

- Use xUnit v3, FluentAssertions, Moq, and Testcontainers PostgreSQL.
- Mirror OpenSpec scenarios with unit or integration tests.
- Reuse shared builders and helpers instead of duplicating large object setup.
- Validate with `dotnet test src/WindLordApi.Tests/WindLordApi.Tests.csproj`.