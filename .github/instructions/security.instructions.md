---
description: Security and secret-handling rules for WindLordApi
applyTo: "**/*"
---

# Security Rules

## Authentication
This repository does not expose a user-facing authentication surface. The primary security concerns are outbound provider credentials, database connection strings, and safe operational behavior.

## Authorization
- Treat changes to secrets, deployment config, or provider access as security-sensitive.
- New integrations or credential flows require explicit review before implementation.

## Secret Handling
- Keep provider API keys and Supabase/PostgreSQL connection strings in user secrets or deployment environment configuration.
- Never commit real secrets to `appsettings*.json`, tests, markdown, or workflow files.
- Do not log raw credentials, full connection strings, or auth headers.

## Data Protection
- Preserve existing data-integrity constraints in EF Core and PostgreSQL mappings.
- Be cautious with logs and diagnostics that could expose station/provider payload details unnecessarily.

## Operational Security
- Health checks and startup logging should report failures without exposing sensitive values.
- Deployment and workflow changes must avoid embedding secrets in scripts or repo-tracked configuration.

## Specifications
When security-sensitive behavior changes, update the relevant OpenSpec artifact first so secret handling, operational assumptions, and validation are explicit.