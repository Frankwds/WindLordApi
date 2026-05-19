---
description: Security rules for secrets, external integrations, and operational changes in WindLordApi
applyTo: "**/*"
---

# Security Rules

## Authentication

WindLordApi is a background worker and does not expose a public application authentication surface. Security work in this repository centers on provider credentials, connection strings, deployment secrets, and safe handling of external inputs.

## Authorization

- Treat access to workflow files, appsettings, and deployment scripts as privileged changes.
- Changes that affect secret loading or runtime credentials require explicit review.

## Input Validation

All external provider input MUST be validated or normalized before it is persisted:
- Validate configuration through strongly typed options and startup checks.
- Validate provider payload assumptions in clients and mapping services before writing shared models.
- Reject or guard against malformed timestamps, directions, and station identifiers.

## Secrets & Credentials

- NEVER hardcode secrets, API keys, passwords, or tokens in source code.
- NEVER log connection strings, provider keys, or secret-bearing payloads.
- Use .NET user secrets for local development and deployment-managed configuration for non-local environments.
- When adding a new provider secret, document where it is loaded and which service consumes it.

## Data Protection

- Treat station and location data as operational data that still deserves least-privilege handling.
- Keep database writes behind EF Core or repository abstractions.
- Preserve transport security assumptions of upstream HTTP clients; do not weaken TLS or certificate validation.

## Common Vulnerabilities To Prevent

- SQL Injection: use EF Core and repository abstractions rather than raw string-built SQL.
- Sensitive Logging: keep exception and request logging scrubbed of secrets.
- Mass Assignment: map provider DTOs explicitly into internal models.
- Path Traversal: validate any future file-path inputs in worker or deployment changes.

## Dependency Security

- Treat workflow changes and new package references as security-sensitive.
- Review new dependencies for maintenance, versioning, and need before adding them.
- Update vulnerable dependencies through normal review rather than ad hoc local-only fixes.