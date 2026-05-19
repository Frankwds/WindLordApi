---
name: brownfield-distill
description: 'Distill a legacy or brownfield repository into high-signal AI context files. Use when onboarding AI to an undocumented repo, capturing intent, canonical language, and unwritten rules, creating a root CONTEXT.md, or adding module-level CONTEXT.md files that document only decision-affecting constraints, invariants, hazards, and terminology.'
argument-hint: 'Optional scope such as a repo area, package, or module to distill first.'
---

# Brownfield Distill

Create layered, load-friendly repository context for AI work in legacy codebases.

The goal is not to summarize the repo. The goal is to preserve only the context that would change an agent's decisions while editing, testing, reviewing, or migrating code.

When the repository has meaningful domain language, preserve that language with the same rigor as technical constraints. Fuzzy terms create bad edits.

Prefer `CONTEXT.md` as the primary output surface. If the repo already uses `AGENTS.md`, ADRs, or other architecture docs, inspect and respect them, then write to `CONTEXT.md` when that is the local convention or the explicit user request.

## Default Output

- A repo-root `CONTEXT.md` with global intent, non-obvious rules, risky areas, terminology, and navigation to any local guidance that matters
- Additional local `CONTEXT.md` files only for major subtrees with their own rules, invariants, hazards, terminology, or validation expectations

## Core Standard

Every sentence in a `CONTEXT.md` file should earn its keep.

Keep information that is:

- hard to infer quickly from file names or a short code read
- likely to change implementation, testing, review, or migration decisions
- specific to this repository or subtree rather than generic engineering advice
- tied to a real invariant, operational behavior, integration quirk, or validation rule

Cut information that is:

- obvious from the directory tree or project names
- a restatement of class, folder, or package names
- generic best practice with no repo-specific consequence
- descriptive but not decision-affecting

Use this deletion test everywhere: if removing a sentence would not materially reduce the quality of future engineering decisions, omit it.

## Language Discipline

Treat language as a first-class artifact, not a decorative section.

- Pick one canonical term for each important concept
- Record aliases or overloaded terms to avoid only when they cause confusion
- Keep definitions tight, usually one sentence
- Flag ambiguities explicitly instead of smoothing them over
- Show relationships between terms when that teaches faster than longer prose
- Exclude generic programming vocabulary

Do not let `CONTEXT.md` become a mixed pile of glossary, implementation notes, and architecture trivia. Keep language crisp. Move hard-to-reverse design choices to ADRs when appropriate.

## When To Use

Use this skill when:

- a legacy repo has important tribal knowledge that is not written down
- AI can read the code but not the real intent, constraints, or team standards
- new chats repeatedly need the same onboarding context
- a repo needs root guidance plus module-level local guidance
- the codebase has different rules in different areas and one root file would be too vague

## Non-Goals

Do not:

- rewrite the README as `CONTEXT.md`
- produce an exhaustive architecture document
- create local files for every folder
- mirror the directory tree in prose
- turn weak inferences into facts
- interrogate the user to fill a template

## Operating Rules

### Evidence first

Before asking the user anything, exhaust cheap evidence:

- code and tests
- existing docs and comments
- build, CI, and configuration files
- naming consistency across the repo
- call sites, data flow, and runtime clues
- git history or blame when likely to clarify intent

Treat runnable behavior and enforced contracts as the primary source of truth when docs conflict with the repo. Use docs to recover intent, explain deliberate choices, or flag drift.

### Ask rarely

For each candidate fact, reason on two axes:

- Confidence: `confirmed`, `strongly inferred`, `weakly inferred`, `unknown`
- Impact: `high`, `medium`, `low`

Only ask the user when impact is `high` and confidence is below `strongly inferred`.

Default question budget: `0-3` questions for a normal pass.

Prefer an open question or flagged ambiguity over an interruption when the impact is only `medium`.

Only interrupt for high-impact uncertainty such as:

- invariants and unacceptable regressions
- deliberate oddities versus accidental legacy mess
- source-of-truth or ownership boundaries
- runtime or operational constraints not visible in code
- data semantics or external compatibility contracts
- validation expectations that change what evidence counts as trustworthy
- canonical language and ambiguous terms
- temporary workaround versus intended direction

### Layer only on divergence

Put repo-wide rules in the root `CONTEXT.md`.

Create a local `CONTEXT.md` only when the subtree has at least one of these:

- distinct business language or overloaded terms
- distinct runtime, framework, or deployment model
- distinct test or validation strategy
- local invariants or integration contracts
- high-risk legacy behavior or migration traps

Do not create a local file if it would only describe folder contents or restate parent guidance.

If the only durable value is terminology, that is enough to justify a local file.

### Prefer incremental updates

If `CONTEXT.md` files already exist, update the nearest relevant file instead of regenerating everything.

Promote common rules upward. Push exceptions downward.

## Procedure

### 1. Discover existing context

Inspect the existing context surface before writing:

- `CONTEXT.md`
- `AGENTS.md`
- `CLAUDE.md`
- `README.md`
- `CONTEXT-MAP.md`
- `docs/adr/`
- contribution docs
- lint, test, build, and CI configuration
- package or workspace manifests

### 2. Map the real shape

Identify only the structure that affects change safety:

- top-level apps, packages, services, modules, or layers
- shared infrastructure versus local business logic
- ownership seams and integration seams
- unusually risky or coupled code paths

For monorepos, start with one root `CONTEXT.md`. Add local files only for bounded contexts with real divergence. If the repo is too large for one pass, distill one high-value subtree first and record the remaining gaps.

### 3. Extract missing knowledge

Capture the facts an agent is least likely to infer correctly:

- product intent and business purpose
- invariants that must not break
- canonical terminology and flagged ambiguities
- behaviors that look safe to simplify but are intentionally complex
- operational or integration quirks
- test and validation expectations
- acceptable shortcuts versus forbidden changes
- recurring traps, historical quirks, and migration warnings

If a point is uncertain, either:

- label it as an open question
- mark it as a flagged ambiguity
- omit it

Do not present weak inference as fact.

Prioritize findings in this order:

1. invariants and contracts
2. canonical terms and ambiguities
3. intentionally complex behavior that looks accidental
4. operational and integration quirks
5. validation expectations
6. minimal navigation help

### 4. Decide the layering

Use this branching logic:

- repo-wide rule: root `CONTEXT.md`
- subtree-specific rule: local `CONTEXT.md`
- surprising, hard-to-reverse trade-off: prefer an ADR over bloating `CONTEXT.md`
- no divergence: inherit from the nearest ancestor file

Assume the closest `CONTEXT.md` supplements and, when necessary, overrides broader guidance above it.

### 5. Write the root file

Use this outline as a menu, not a template:

```md
# CONTEXT.md

## Purpose
- What this system does
- What matters most when changing it

## Language
- Canonical domain terms
- Aliases or overloaded words to avoid

## Relationships
- How the important terms relate

## Flagged Ambiguities
- Terms used inconsistently and the chosen resolution

## Architecture
- Only the boundaries that affect change safety
- Minimal navigation to the few places an agent needs to know

## Global Rules
- Repo-specific implementation rules
- Testing and validation expectations
- Forbidden changes and safety constraints

## Commands
- Only commands an agent is likely to need

## Hot Spots
- Fragile areas, migration traps, historical quirks

## Local Guidance Map
- Which subtrees have their own local `CONTEXT.md`
```

For language-heavy repos, put the language sections first and keep the operational sections brief.

### 6. Write local files only where needed

Use this outline as a menu, not a template:

```md
# CONTEXT.md

## Scope
- Only if ownership is non-obvious or easy to violate

## Language
- Canonical local terms
- Aliases to avoid

## Relationships
- Local concept relationships when they matter

## Flagged Ambiguities
- Local terminology conflicts and their resolution

## Local Intent
- Why this area exists
- What changes here tend to break

## Structure
- Only the entrypoints, layers, or dependencies that are hard to infer or easy to misuse

## Local Rules
- Local conventions, invariants, and contracts

## Validation
- Tests or checks to run for changes here

## Watchouts
- Legacy traps, fragile assumptions, known pain points
```

Do not create a local file unless it contains at least one concrete local rule, watchout, terminology distinction, or validation note.

## Worked Example

Observed facts:

- `billing/` uses the term `settlement`, but `payments/` and the README call the same thing `payout`
- only `billing/` has ledger invariants and replay tests
- the rest of the repo uses the standard test command

Good distillation:

- root `CONTEXT.md`: define the repo purpose, declare `settlement` as the canonical billing term if the code and tests support it, note that `billing/CONTEXT.md` exists
- `billing/CONTEXT.md`: explain the `settlement` term, record the ledger invariants, and list the replay validation command

Bad distillation:

- adding local files for every package
- copying folder descriptions into each file
- listing both `settlement` and `payout` without choosing a preferred term

## Deliverables

At completion, return:

- which `CONTEXT.md` files were created or updated
- any high-impact ambiguities left unresolved
- why local files were created, or why they were intentionally not created

## Validation Checklist

Before finishing, verify:

- the root file orients a new agent without drowning in detail
- every local file documents real local divergence
- commands and paths actually exist
- terminology matches the code and existing docs, or ambiguities are flagged explicitly
- global rules are not duplicated mechanically into local files
- structure sections do not paraphrase the directory tree
- uncertain claims are marked as open questions or ambiguities
- each bullet would change how a competent agent edits, validates, or reviews code here
- the user was interrupted only for genuinely high-impact unknowns

## Anti-Patterns

Avoid:

- copying the README into `CONTEXT.md`
- writing local files for every folder
- documenting speculative architecture as fact
- filling files with generic best practices
- hiding important constraints in narrative paragraphs
- adding terminology that only repeats model or class names
- using a fixed section template when half the sections would be boilerplate
- listing synonyms without choosing a canonical term

## Completion Criteria

This skill is complete when:

- a fresh agent can understand the repo's purpose and guardrails from the root file
- a fresh agent can find the right local guidance for the touched area
- the local guidance footprint stays intentionally sparse
- repeated tribal-knowledge prompts are no longer needed for normal work
- future discoveries have an obvious home in the nearest relevant `CONTEXT.md`