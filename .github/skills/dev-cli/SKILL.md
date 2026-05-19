---
name: dev-cli
description: Dev CLI commands, flags, and developer workflows. Use when choosing which dev command to run, understanding available options, or integrating CLI calls into automation.
---

# Dev CLI Reference

`dev-cli` is the platform CLI. Install as a .NET tool; run from the repository root.

---

## `dev-cli archetype` / `dev-cli arc`

Scaffold code and infrastructure from reusable archetype templates defined in `.dev/archetypes/`.

### `dev-cli arc new <archetype> [parameters]`

Generate files from an archetype.

```
dev-cli arc new <archetype>
  --project, -p <name>      Target project (required when archetype sets requiresProject)
  --namespace, -n <ns>      Root namespace (required when archetype sets requiresNamespace)
  --schema, -s <path>       JSON schema for type inference (required when archetype sets requiresSchemaInference)
  --dry-run                 Preview output without writing files
  --verbose, -v             Show detailed generation log
  --<ParamName> <value>     Archetype-defined parameters (vary by archetype)
```

### `dev-cli arc interactive [archetype]`

Prompt for all inputs via interactive console. Omit archetype name to select from a list.

### `dev-cli arc list`

List all available (non-internal) archetypes.

### `dev-cli arc info <name>`

Show details for a specific archetype: description, parameters, files, and actions.

### `dev-cli arc validate <path>`

Validate an archetype definition for schema and template correctness. `<path>` is the path to the archetype directory or its `archetype.json` file. Run before committing a new archetype.

---

## `dev-cli spec compile`

Compile specification primitives by creating **hard links** from `.dev/primitives/` to AI context directories (`.github/`, `.claude/`, `.agents/`, `.codex/`). Run once after adding a new skill directory, instruction, agent, prompt, or chat mode. After the initial run, edits to existing files propagate instantly to all target directories — no recompile needed. Files are committed as real content so cloud agents and teammates without dev-cli still see them.

```
dev-cli spec compile
  --target <target>   Output target: Copilot | Claude | Codex | All (default: All)
  --dry-run           Preview changes without writing files
  --verbose           Show detailed link operations
  --repair            Repair broken hard links only (used automatically by git hooks)
```

**What it produces:**

- `.github/instructions/*.md` — instruction files (hard-linked)
- `.github/agents/*.agent.md` — agent and chat mode files (hard-linked)
- `.github/prompts/*.prompt.md` — prompt files (hard-linked)
- `.github/skills/<name>/` — skill directory containing per-file hard links
- `.github/skills/archetype-catalogue/SKILL.md` — auto-generated catalogue of all archetypes _(generated, not linked)_
- `.claude/commands/`, `.claude/agents/`, `.claude/skills/` — same primitives for Claude
- `.agents/skills/`, `.codex/agents/` — same primitives for Codex

**Git hooks:** `spec compile` installs a `post-checkout` git hook that automatically runs `spec compile --repair --quiet` after every `git checkout`. This re-establishes hard links that git replaced with regular file copies, keeping all targets live without any manual action.

Run after adding any new skill directory, instruction, agent, prompt, or chat mode file to `.dev/primitives/`. Pack install/update/remove trigger this automatically.

---

## `dev-cli init`

Emit CLI-owned primitives to the workspace. Run once after installing or upgrading the CLI.

```
dev-cli init
```

Writes to `.dev/primitives/`:

- `agents/` — agent definition files
- `instructions/` — instruction files
- `prompts/` — prompt files
- `chatmodes/` — chat mode files
- `skills/<name>/` — skill subdirectories

Existing files are always overwritten (CLI owns these files).

---

## `dev-cli mcp start`

Start the MCP (Model Context Protocol) server over stdio. Exposes all archetypes as MCP tools so IDE extensions and AI agents can invoke scaffolding programmatically.

```
dev-cli mcp start
```

---

## `dev-cli accelerator upgrade` / `dev-cli acc upgrade`

Pull a tagged version from an upstream accelerator repository into this repo as a merge commit.

```
dev-cli acc upgrade <version>
  --upstream, -u <url>    Upstream Git remote URL (required on first use)
```

Creates a dedicated upgrade branch, adds the upstream remote, fetches the tag, and merges it.

---

## `dev-cli version` / `dev-cli info`

```
dev-cli version    Show CLI assembly version and accelerator version (from .version file if present)
dev-cli info       Show version plus working directory and runtime details
```

---

## Conventions

- Always run from the **repository root**
- Archetype names are **kebab-case** (e.g. `servicebus-consumer`)
- Use `--dry-run` to preview any destructive or generative command before committing
- After adding or editing an archetype, run `dev-cli arc validate <name>` then `dev-cli spec compile`
- After adding a new skill folder or primitive file to `.dev/primitives/`, run `dev-cli spec compile` once; subsequent edits to that file are live immediately
