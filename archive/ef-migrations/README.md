# Archived EF Migrations

These files are retained as historical reference only.

They are no longer part of the active `WindLordApi.Data` project build, and they are not the source of truth for future database changes.

The active database schema and version history are managed in the upstream Supabase database repository. This repository now treats EF Core as a runtime mapping layer plus startup schema-contract validation for the tables and views WindLordApi actually depends on.

If WindLordApi needs new database capabilities, update the upstream schema first, then update the EF model and startup contract checks here to match the live database contract.
