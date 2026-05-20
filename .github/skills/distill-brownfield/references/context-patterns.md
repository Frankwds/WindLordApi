# Context Patterns

Use this file only when you need structure help while writing or reviewing `CONTEXT.md` files.

## Root File Menu

Use only the sections that carry real signal.

```md
# CONTEXT.md

## Purpose
- What the system does
- What matters most when changing it

## Language
- Canonical terms
- Aliases or overloaded words to avoid

## Relationships
- How the important terms relate, if that reduces confusion

## Flagged Ambiguities
- Inconsistent terms or boundaries that remain unresolved

## Architecture
- Only boundaries that affect change safety
- Minimal navigation to the critical areas

## Global Rules
- Repo-specific implementation constraints
- Testing and validation expectations
- Forbidden simplifications or unsafe edits

## Commands
- Only commands likely to be reused

## Hot Spots
- Fragile areas, migration traps, historical quirks

## Local Guidance Map
- Which subtrees have their own local `CONTEXT.md`
```

## Local File Menu

Create a local file only when the subtree has genuine local rules.

```md
# CONTEXT.md

## Scope
- Include only when ownership or boundary is easy to violate

## Language
- Canonical local terms
- Aliases to avoid

## Relationships
- Local concept relationships that affect edits

## Flagged Ambiguities
- Local term conflicts or unresolved boundaries

## Local Intent
- Why this area exists
- What changes here usually break

## Structure
- Only the entrypoints, seams, or dependencies that are easy to misuse

## Local Rules
- Local invariants and contracts

## Validation
- Tests or checks to run for changes here

## Watchouts
- Legacy traps, brittle assumptions, migration hazards
```

## Layering Checklist

- Promote common rules upward.
- Push exceptions downward.
- Prefer one root file plus sparse local files.
- Skip a local file if it would only paraphrase the tree.
- Allow terminology alone to justify a local file when it changes edits.

## Sentence Filter

Keep a sentence only if removing it would materially reduce the quality of future engineering decisions.

Delete sentences that are:

- obvious from file names or package names
- generic best practice with no repo-specific consequence
- descriptive but not decision-affecting
- speculative claims dressed up as facts

## Compact Example

Observed facts:

- `billing/` uses `settlement`
- `payments/` and README use `payout`
- only `billing/` has ledger invariants and replay tests

Good output:

- root `CONTEXT.md`: choose the canonical repo term that code and tests support, note that `billing/CONTEXT.md` exists
- `billing/CONTEXT.md`: define the local term, record the ledger invariants, and list the replay validation command

Bad output:

- create local files for every package
- list both terms without choosing one
- copy folder descriptions into prose