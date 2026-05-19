# Security Rules

- Never hardcode provider keys, connection strings, or tokens.
- Never log secret-bearing configuration or raw credential values.
- Validate provider input before persisting normalized models.
- Treat workflow, appsettings, and deployment changes as security-sensitive.