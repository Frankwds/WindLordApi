---
name: distill-brownfield
description: Distill a legacy or brownfield repository into high-signal CONTEXT.md files that preserve only decision-affecting knowledge for future AI work. Use when onboarding AI to an undocumented codebase, capturing repo intent, canonical domain language, invariants, hazards, validation expectations, or unwritten rules, and when creating or updating a root CONTEXT.md plus sparse local CONTEXT.md files only where subtree behavior materially diverges.
---

# Distill Brownfield

Create layered repository context for future agents without turning the result into a README clone or architecture dump.

## Default Output

- Create or update a repo-root `CONTEXT.md`.
- Add local `CONTEXT.md` files only for subtrees with real divergence in terminology, invariants, runtime model, validation, or migration hazards.
- When the repo is too large for one safe pass, distill one high-risk subtree first, record uncovered areas, and return the next recommended passes instead of writing a shallow whole-repo summary.
- Return the files changed, unresolved high-impact ambiguities, and why any local files were added or intentionally skipped.

## Follow This Workflow

1. Inspect existing context before writing anything.
2. Map only the boundaries that change edit safety.
3. Extract facts that would materially change implementation, testing, review, or migration decisions.
4. Classify each important point by confidence: `confirmed`, `strongly inferred`, `weakly inferred`, or `unknown`.
5. Ask the user only when a point is high impact and below `strongly inferred`.
6. If the repo is too large for one safe pass, choose one high-risk or high-change subtree, distill that slice first, and record the remaining uncovered areas.
7. Write the root `CONTEXT.md` first, even for a partial pass, and state the current coverage boundary clearly.
8. Add local `CONTEXT.md` files only where the local rules truly diverge.
9. Validate that every sentence earns its keep.

## Inspect Cheap Evidence First

Inspect the nearest evidence before asking questions:

- existing `CONTEXT.md`, `AGENTS.md`, `CLAUDE.md`, README, ADRs, contribution docs
- tests, fixtures, CI, build, lint, and workspace manifests
- code paths that enforce invariants or external contracts
- naming consistency across modules and call sites
- git history or blame only when intent is still unclear

Treat runnable behavior and enforced contracts as the source of truth when docs drift.

Treat existing human-authored instruction files such as `AGENTS.md`, `CLAUDE.md`, and repo contribution guidance as normative for workflow and constraints. Use `CONTEXT.md` to supplement them with repo facts, local language, invariants, and hazards rather than silently overriding those instructions.

## Decide What Belongs

Keep only information that is hard to infer quickly and would change future decisions.

Prefer these categories, in this order:

1. invariants and compatibility contracts
2. canonical terms and flagged ambiguities
3. intentional complexity that looks accidental
4. operational quirks and integration hazards
5. validation expectations
6. minimal navigation to the few places that matter

Cut anything that merely restates directory names, generic engineering advice, or obvious implementation details.

## Control Question Volume

Keep the default question budget at `0-3`.

Interrupt only for high-impact unknowns such as:

- unacceptable regressions or safety constraints
- ambiguous source-of-truth boundaries
- data semantics not visible in code
- runtime or operational constraints hidden outside the repo
- canonical language that changes how edits should be made

For medium-impact uncertainty, prefer a flagged ambiguity over an interruption.

## Layer Only On Real Divergence

Write repo-wide rules in the root file.

Create a local `CONTEXT.md` only when the subtree has at least one durable local difference:

- different business language or overloaded terms
- different runtime, framework, or deployment model
- different validation or test expectations
- local invariants or integration contracts
- legacy traps or migration hazards that do not apply elsewhere

Do not create a local file just to describe folder contents.

If the only durable value is a surprising, hard-to-reverse trade-off, prefer an ADR or the existing architecture record over bloating `CONTEXT.md`.

## Write With Language Discipline

- Choose one canonical term for each important concept within its scope.
- When different bounded contexts legitimately use different local terms, preserve the local canonical term in the nearest relevant `CONTEXT.md` and map cross-scope aliases at the root only when the distinction affects edits.
- Record aliases only when they cause confusion.
- Flag inconsistent usage explicitly instead of smoothing it over.
- Keep definitions tight and decision-oriented.
- Preserve domain language with the same care as technical rules.

## Write The Files

Use the root file to orient a fresh agent quickly: purpose, language, boundaries, global rules, validation, hotspots, a map of any local guidance, and the current coverage boundary when the pass is partial.

Use local files to document only subtree-specific intent, terminology, rules, validation, and watchouts.

Keep uncertainty visible in the files. For any non-obvious claim that is not `confirmed`, either flag it in a `Flagged Ambiguities` section or attach a lightweight evidence breadcrumb so a future agent can re-check the source quickly.

Read [references/context-patterns.md](references/context-patterns.md) when you need a menu of root/local section shapes, a layering checklist, or a compact example.

## Validate Before Finishing

Confirm all of the following:

- every bullet would change a competent agent's decisions
- the root file is enough to orient a new agent without drowning it in detail
- partial passes state what was covered and what remains uncovered
- each local file documents real divergence rather than directory structure
- commands and paths mentioned in the output actually exist
- uncertain claims remain visibly flagged, and non-obvious claims keep a lightweight evidence breadcrumb when useful
- global guidance is not duplicated mechanically into local files

## Avoid These Failure Modes

- rewriting README material as `CONTEXT.md`
- creating local files for every package or folder
- documenting speculative architecture as fact
- listing synonyms without choosing a canonical term
- using a fixed section template when half the sections are boilerplate

## Stop When This Is True

- a fresh agent can understand the repo's purpose and guardrails from the root file
- a fresh agent can find the right local guidance for the touched area
- the local guidance footprint stays intentionally sparse
- any partial pass makes uncovered areas and next recommended passes explicit
- future discoveries have an obvious home in the nearest relevant `CONTEXT.md`

## Return

Return:

- which `CONTEXT.md` files were created or updated
- any unresolved high-impact ambiguities
- any uncovered areas and the next recommended distillation passes when the repo was too large for one safe pass
- why local files were created, or why they were intentionally not created
