# Security

## Secret Handling
- Keep provider API keys and Supabase/PostgreSQL connection strings in user secrets or deployment environment configuration.
- Never commit real secrets to config files, workflow files, docs, or tests.
- Do not log raw credentials, full connection strings, or auth headers.

## Operational Security
- Treat new integrations and credential flows as security-sensitive changes.
- Health checks and startup logging should report failures without exposing sensitive values.
- Deployment changes must avoid embedding secrets in scripts or tracked files.

## Data Protection
- Preserve existing EF Core and PostgreSQL constraints that protect data integrity.
- Be careful with diagnostics that could expose unnecessary provider payload details.

## OpenSpec
Call out secret, provider, or config impact in proposals and designs before implementation starts.