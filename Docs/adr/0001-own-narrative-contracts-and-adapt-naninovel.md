---
status: accepted
---

# Own narrative contracts and adapt Naninovel

Levity owns the game-facing narrative vocabulary, lifecycle, flow integration, unified save, and settings contracts, while Naninovel remains the default narrative backend and `.nani` remains the source authoring format. This preserves Naninovel's mature dialogue and presentation capabilities without allowing its types and product boundaries to spread through game logic; replacing Naninovel is an option enabled by the boundary, not a planned rewrite.

## Considered Options

- Expose Naninovel directly throughout the framework: lowest initial effort, but couples game flow, saves, and settings to one vendor API.
- Reimplement or fork Naninovel: maximizes control, but duplicates a script language, editor, runtime, localization, rollback, and presentation system without a current product need.
- Own stable Levity contracts and isolate Naninovel in an adapter: chosen because it gives the game a coherent architecture while retaining the existing authoring workflow.

## Consequences

- Game flow and gameplay code use narrative sequence IDs, typed inputs and outcomes, and never Naninovel types.
- Naninovel-specific features remain available through explicit adapter extensions until they prove general enough for the core contract.
- Unified save and settings are coordinated by Levity; the backend contributes state and applies mapped settings.
- Core, flow, adapter, and editor dependencies are enforced as separate Unity assemblies.
