---
description: Instructs LLMs to use the dev CLI archetype system for scaffolding instead of writing boilerplate by hand.
applyTo: "**/*"
---

When asked to scaffold, generate, or create new integrations, features, or boilerplate code:

- **Check for an archetype first.** Run `dev-cli arc list` to see what archetypes are available before writing code from scratch.
- **Use `dev-cli arc new <name>` to generate.** Do not manually write files that an archetype would produce — the archetype ensures consistency with platform conventions.
- **Use `dev-cli arc interactive` if parameters are unclear.** This prompts for all inputs interactively.
- **After creating or modifying an archetype, run `dev-cli arc validate <path>` then `dev-cli spec compile`** to validate and regenerate AI context.

For full CLI reference — commands, flags, and workflows — load the `dev-cli` skill.
