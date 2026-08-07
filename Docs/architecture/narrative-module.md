# Narrative Module Architecture

## Status

Accepted design, 2026-08-06.

Implementation status: target architecture under migration. Narrative Core, the Flow tracer bullet, the Naninovel runtime adapter, transactional unified load, save-availability enforcement, and application composition are implemented. Narrative Editor tooling and the remaining production migrations are not yet implemented.

This document is the architectural authority for integrating narrative and presentation into Levity Framework. The superseded Naninovel integration roadmap has been removed; requirements and implementation status are tracked in GitHub Issues.

## Intent

Naninovel should satisfy the game's complete narrative needs: dialogue, choices, branching, localization, and presentation authored in `.nani`. Levity should internalize narrative as a coherent framework module without copying Naninovel or allowing Naninovel APIs to become the framework's domain model.

The design optimizes for reusable game-flow integration, unified persistence and settings, testability without booting Naninovel, and gradual migration from the existing integration. It does not create a replacement narrative engine.

## Ownership Boundary

Levity owns:

- when a narrative sequence starts and how it participates in game flow;
- stable sequence identity, validated inputs, and typed outcomes;
- narrative-session lifecycle, concurrency policy, cancellation, and failure semantics;
- gameplay-command ports and committed-side-effect tracking;
- unified save orchestration and save availability;
- the canonical game settings model;
- project-level validation and diagnostics.

Naninovel owns:

- `.nani` authoring and execution;
- dialogue, choices, branching internal to a sequence, text presentation, and cinematic presentation;
- its internal variables and runtime state;
- native localization, audio, text-printer, and choice behavior;
- capture and restoration of its opaque state snapshot.

Game Flow owns bridge-scale orchestration. A branch that only changes dialogue or presentation remains in `.nani`; a branch that changes gameplay, stage routing, or quest state returns a typed narrative outcome and continues in Game Flow.

## Module Structure

```text
Levity.Narrative.Core
├── narrative sequence and session contracts
├── inputs, outcomes, lifecycle, and failure model
├── save availability and backend snapshot contracts
└── settings and gameplay-command ports

Levity.Narrative.Flow
├── play-sequence flow node
├── input and outcome schema integration
└── scene-transition policy

Levity.Narrative.Runtime
├── optional backend registration seam
└── module plus unified-save binding

Levity.Narrative.Naninovel
├── Naninovel backend adapter
├── sequence registry mapping
├── custom command adapters
├── save and settings mapping
└── Naninovel-specific extension surface

Levity.Narrative.Editor
├── sequence, entry-point, parameter, and outcome validation
├── missing-backend diagnostics
└── flow-node to .nani navigation
```

Unity assembly definitions enforce this direction: Flow depends on Core; Runtime depends on Core and Unified Save; the Naninovel adapter depends on Runtime and Naninovel; Editor tooling depends on the public contracts it validates. Core and Runtime contain no Naninovel types. The adapter assembly uses a package-backed version define and define constraint, so it and its tests leave compilation when Naninovel is absent.

## Runtime Contract

Game code requests a sequence through a use-case-level narrative module interface. A request contains a stable `NarrativeSequenceId`, an optional entry point, validated typed parameters, and an explicit policy when another session is active. The default policy rejects a concurrent request; callers must explicitly wait, replace, or cancel.

A session completes as `Completed`, `Cancelled`, or `Failed`, with an optional typed narrative outcome. Game Flow handles all three explicitly. Missing scripts, invalid parameters, backend initialization failures, and execution failures return a typed failure and produce detailed development diagnostics; they are never silently skipped.

A sequence registry maps stable sequence IDs to backend-specific scripts and entry points. Moving a `.nani` asset therefore does not rewrite game-flow data. Duplicate registration fails explicitly and preserves the original mapping; intentional changes use the separately named replacement API. Input and outcome schemas allow editor validation while preserving backend-agnostic runtime contracts.

## Flow Integration

The primary Flow node plays one narrative sequence and waits for its result. It does not reproduce individual dialogue, choice, or presentation commands. Completed outcomes select their typed gameplay branches; cancellation selects the configured cancellation branch; failures select a failure-code branch before the generic failure fallback. The returned Flow result preserves terminal status, typed outcome or failure, and the selected branch. An unconfigured terminal route is a Flow configuration error.

At scene transitions, Flow explicitly selects one of:

- `CompleteBeforeTransition`, the default;
- `PersistAcrossTransition`, for a deliberately cross-scene session;
- `Cancel`, for an intentional interruption.

The backend never owns the application's scene or stage lifecycle. Levity coordinates input locks, gameplay suspension, camera ownership, and restoration around the narrative session.

## Gameplay Commands and Side Effects

Naninovel custom commands call registered gameplay-command handlers. They do not access `GameRoot`, static service locators, or concrete game services directly. Handlers depend on narrow gameplay-owned ports.

Commands declare one behavioral category:

- `Pure`: observes state without changing it;
- `Stateful`: changes state represented in the unified save;
- `Irreversible`: causes an effect that cannot be replayed or rolled back safely;
- `Presentation`: changes presentation without owning business state.

Stateful command executions receive stable IDs. Levity records committed IDs with the unified save so restoring a dialogue position cannot grant an item, spend currency, or advance a quest twice. Irreversible commands establish explicit safe boundaries. Active presentation commands contribute to save availability.

## Unified Save

Levity initiates and commits the unified save. Gameplay systems and the Naninovel backend contribute versioned state; no contributor writes an independently successful partial save. The complete payload is validated, written to a temporary location, and atomically replaces the prior valid slot only after every contributor succeeds. Save callers receive a typed result with a stable failure category, slot ID, optional contributor ID, diagnostic message, and original exception. The legacy throwing save method remains only as an obsolete compatibility wrapper.

Naninovel 1.20.250922 saves a command-level playback position (`scriptPath`, `lineIndex`, and `inlineIndex`), executed/waiting state, choice state, and service state. Before saving, it completes active asynchronous commands. Consequently, ordinary dialogue and waiting choices can be restored meaningfully, but an in-progress cinematic is not an exact animation-frame snapshot.

Levity aggregates `SaveAvailability` from narrative, gameplay, scene transition, and other active modules. Ordinary dialogue and choices allow saving. Complex cinematic presentation blocks saving with a user-facing reason instead of allowing Naninovel to force the presentation to its completion state. Gameplay may permit saving only at its own checkpoints.

Custom Naninovel commands must correctly observe completion tokens so saving cannot wait indefinitely.

## Unified Settings

Levity's game settings are canonical. The Naninovel adapter maps relevant values into Naninovel:

- audio volume applies immediately;
- text reveal speed applies to the next print operation, matching Naninovel behavior;
- locale changes re-localize the current message and choices without re-executing the script command;
- other narrative settings follow the same one-way authority and explicit mapping.

Naninovel may maintain internal settings objects, but they are not a second source of truth.

## Diagnostics and Editor Validation

Each narrative session emits structured diagnostics containing its session ID, Flow node, sequence ID, mapped script and entry point, inputs, outcome, lifecycle transitions, failure or cancellation reason, save position, committed side-effect IDs, and any reason saving is blocked.

Editor validation detects missing backends, unknown sequence IDs, missing scripts or entry points, invalid parameter types, unhandled outcomes, and unavailable resources. Authors can navigate from a Flow node to the mapped `.nani` source.

## Composition and Migration

`GameRoot` discovers an optional `NarrativeRuntimeBinding` through the backend-neutral Runtime seam. An installed adapter registers one factory before scene load; the binding contributes both its narrative module and its unified-save contributor after `DataService` initializes. The concrete Naninovel adapter never appears in `GameRoot`, and removing its package leaves the rest of the project compilable.

The existing `NaninovelService` and Naninovel-specific stage configuration remain compatibility presentation entry points. They no longer implement the Narrative Backend or duplicate unified-save contribution. Existing references migrate incrementally and are removed after content and callers use the new contracts.

## Verification Strategy

Tests assert observable behavior rather than adapter internals:

- Core contract tests cover session lifecycle, explicit concurrency policies, cancellation, failures, and save availability.
- A fake backend exercises Flow without starting Naninovel.
- Adapter integration tests cover script playback, choices and typed outcomes, settings mapping, and initialization failures.
- Unified-save round trips cover dialogue position, choices, gameplay state, settings, and committed side effects.
- Failure tests prove that a contributor failure preserves the prior valid save.
- Editor tests cover invalid sequence IDs, parameter and outcome schemas, missing scripts, and absent backend packages.
- A package-absent project copy compiles and runs all non-Naninovel EditMode tests.

## First Tracer Bullet

The first vertical slice delivers one Flow node that starts a `.nani` sequence, receives a typed choice outcome, branches into different gameplay paths, saves during ordinary dialogue or a waiting choice, restores into the correct state without repeating gameplay side effects, and applies Levity-owned settings through the adapter.

This slice is complete only when it runs with Naninovel, runs Flow tests against a fake backend, and proves atomic unified-save failure behavior.

## Out of Scope

- Replacing, forking, or reimplementing Naninovel.
- Defining a Levity-specific narrative authoring language or intermediate content format.
- Runtime hot-swapping of narrative backends.
- Reproducing dialogue, choice, or cinematic details as fine-grained Flow nodes.
- Refactoring all existing services away from `GameRoot` and static service location.
- Exact restoration of arbitrary animation frames during cinematic presentation.
- Implementing every feature listed in the legacy Naninovel integration roadmap in the first slice.
