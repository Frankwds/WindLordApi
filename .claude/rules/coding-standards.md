---
paths:
  - "src/**/*"
---

# Coding Standards

- Follow `WindLordApi.<Project>.<Feature>` namespaces.
- Use PascalCase for files, types, and methods; `I` prefixes for interfaces; camelCase for locals and parameters.
- Keep provider-specific logic inside the relevant integration folder.
- Keep persistence behind repositories, data services, and unit-of-work abstractions.
- Use strongly typed options and startup validation for configuration.