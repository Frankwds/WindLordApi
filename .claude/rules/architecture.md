---
paths:
  - "src/**/*"
---

# Architecture Rules

- Worker owns scheduling and orchestration.
- Integrations own provider DTOs, clients, mappings, and options.
- Data owns entities, repositories, transactions, and migrations.
- Keep forecast, station-network, observation, and location-enrichment workflows separate unless OpenSpec changes that behavior.
- Treat batching, schedule changes, and schema changes as design-level concerns.