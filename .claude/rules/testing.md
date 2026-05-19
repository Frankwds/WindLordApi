---
paths:
  - "src/WindLordApi.Tests/**/*"
---

# Testing

## Stack
- xUnit v3
- FluentAssertions
- Moq
- PostgreSQL Testcontainers
- EF Core InMemory for narrow unit-level helpers where provider semantics are not under test

## Structure
- Keep unit tests under `src/WindLordApi.Tests/Unit`.
- Keep integration tests under `src/WindLordApi.Tests/Integration`.
- Reuse shared helpers under `src/WindLordApi.Tests/Helpers`.

## Expectations
- Every behavior change needs a focused validation step.
- Prefer integration tests for repository, mapping-constraint, view, and multi-layer workflows.
- Prefer unit tests for isolated service, mapping, and scheduler logic.
- Mirror `Given/When/Then` scenarios from `openspec/specs/` when behavior changes.