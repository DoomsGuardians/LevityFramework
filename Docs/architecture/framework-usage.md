# Levity Framework Usage Architecture

## Status

Accepted design, 2026-08-06.

Implementation status: incremental architecture under migration. Composition and Unified Save are implemented Core capabilities; the remaining sections describe a mixture of implemented behavior and migration targets. See the maturity checklist below, `Docs/README.md`, and the root `README.md` for the current implementation boundary.

This document defines how Levity Framework is installed, composed, extended, tested, and used to build games. The narrative subsystem is specified in `Docs/architecture/narrative-module.md`; that design remains authoritative for Naninovel-facing contracts.

## Product Position

Levity is an opinionated Unity framework for PC single-player games. Its primary proving ground is a narrative-driven mission action game in the style of flight combat or mech combat, with mission briefings, in-mission dialogue, full visual-novel sequences, hangar or base progression, and unified saves.

The same Core must remain useful for Game Jam projects such as tactical, strategy, and RPG games. Genre-specific concepts such as aircraft, mechs, weapons, objectives, grids, turns, quests, and character progression belong to game projects until repeated use proves a stable cross-project abstraction.

Levity does not currently target multiplayer, server synchronization, live content updates, or runtime backend hot-swapping. Future Mod support is preserved through stable IDs, data separation, resource-loading ports, and content-version metadata rather than a prematurely implemented Mod platform.

## Distribution and Presets

The framework is consumed as an internal Unity Package with a separate starter/sample experience. Its standard technology stack includes Odin, DOTween, and Naninovel, while code boundaries still isolate integrations from Core.

Two supported presets define the normal entry points:

- **Minimal**: composition, lifecycle, Stage, Input, UI, events, resources, and persistence. Naninovel may be installed but is not initialized or configured as a project requirement.
- **Narrative Action**: Minimal plus Game Flow, Naninovel, presentation coordination, narrative settings, unified narrative saves, and the reference vertical slice.

Strategy, tactical, and RPG usage begins as small recipes and samples. They do not introduce speculative genre frameworks into Core.

## Capability Maturity

Framework code is classified explicitly:

- **Core**: stable, used by the supported path, tested through public behavior, documented, and validated.
- **Supported Module**: optional, focused, and validated by at least one real scenario.
- **Toolkit**: independent utility code that does not own global lifecycle or shape Core dependencies.
- **Experimental**: available for exploration but free to change and not recommended as a production foundation.
- **Deprecated**: retained temporarily with a documented migration path.

The existence of source code does not imply production support. FSM, pooling, RoleSystem, MonoItemSystem, command ScriptableObjects, singleton helpers, and generic utilities are reviewed and classified rather than deleted solely because they are unused. Utilities with duplicated responsibilities, global hidden ownership, or known correctness problems cannot remain Supported without repair and tests.

### Current Maturity Checklist

| Capability | Maturity | Evidence and remaining boundary |
| --- | --- | --- |
| Composition and deterministic lifecycle | **Core** | Public behavior tests cover dependency validation, ordered Initialize/Start, and reverse-order Shutdown. `GameRoot` retains an `ILogic` compatibility path while new modules use explicit composition. |
| Unified Save | **Core** | Versioned contributors commit gameplay and narrative state through a durable candidate and atomic slot replacement. Save and load return typed results; failed capture, restore, or replacement preserves or rolls back to the prior valid state. |
| Narrative Core and Flow tracer bullet | **Core** | Backend-neutral sessions, stable Sequence IDs, typed terminal results, Flow routing, and committed Gameplay Command identities are covered without requiring Naninovel. |
| Naninovel adapter | **Supported Module** | A real `.nani` choice, typed outcome, save blocking, and play/save/load/no-repeat path run through the production `GameRoot`. Remaining adapter ownership and concurrency hardening are tracked separately. |
| Narrative Editor tooling and independent workspaces | **Target** | Validation, navigation, Narrative Workspace, Stage Workspace, and Integration Workspace remain planned work. |
| Setup presets, runtime-state coordination, input, UI, resources, audio, and diagnostics | **Target** | These sections remain architectural direction until each capability receives its own public behavior tests and maturity review. |

## Composition and Lifecycle

`GameRoot` executes a stable bootstrap protocol; it is not edited to add game features. Each game owns a Composition/Installer that registers modules and startup configuration explicitly.

Initialization uses deterministic phases:

```text
Register → Initialize → Start
```

Shutdown follows the reverse ownership order. Missing dependencies fail before the game enters a partially initialized state. Pure C# modules receive narrow dependencies through construction. Unity objects receive them through an explicit initialization contract. Concrete `GameRoot` fields, static service location, and base-class lookup are not parallel supported dependency paths.

The standard lifecycle vocabulary is:

- **Service**: process-level infrastructure without authoritative game-flow state.
- **System**: cross-Stage game state and rules.
- **Manager**: a Stage/Scene-owned coordinator that unregisters and releases on exit.
- **Game Mode**: the top-level game-flow state.

The name `Manager` remains for compatibility, but its scene-scoped ownership is mandatory. Core does not assume that a player character or real-time battle always exists.

## Project Setup

An editor Setup tool creates visible, game-owned assets:

- Bootstrap Scene and Composition;
- canonical Input Action Asset;
- UI Root, UI Camera, EventSystem, and input module;
- startup configuration and Stage registry;
- default Stage and Game Mode;
- default fade, Audio Mixer, and save location;
- selected preset configuration.

Setup is idempotent. It previews changes, preserves user customization, records undo or a backup for asset mutations, repairs missing generated defaults, and handles upgrades through explicit migrations. Runtime code never silently creates project structure or guesses modules by scanning scenes.

The Game Jam target is a usable project within 10–30 minutes. Jam and Production modes use the same runtime API; Jam mode may reduce validation friction, while Production mode enables the full validation and test gates.

## Stable Identity and Configuration

Stage, Game Mode, Window, Narrative Sequence, and Audio Event use distinct strong ID types backed by stable serializable values. Core does not require projects to edit global enums or pass interchangeable raw strings.

Configuration is referenced explicitly from Composition and registries, not loaded from fixed paths. ScriptableObject/Odin assets remain the preferred editor authoring format, but runtime systems consume read-only data models and stable IDs. This leaves room for JSON, AssetBundle, Addressables, YooAsset, or local Mod content providers without committing to one today.

## Stage and Game Flow

A Stage is a top-level enterable game phase such as a base, mission, narrative chapter, or result screen. A Stage Definition contains only stable identity, Scene reference, Game Mode, startup Flow, scene installer, and optional preload IDs.

Stage transition is a single-flight asynchronous transaction:

```text
Validate
→ Acquire loading, input, and time leases
→ Stop Flow
→ Exit Game Mode and Managers
→ Unload the old Stage
→ Load the Scene
→ Install the new Stage
→ Enter the Game Mode
→ Start Flow
→ Mark Ready
→ Release leases
```

The result is `Completed`, `Cancelled`, or `Failed`. Completion is published only after the new Stage is fully ready. Repeated or concurrent transition requests cannot leave a half-initialized Stage.

Game Flow coordinates mission phases, objectives, narrative sequences, results, and Stage transitions. It invokes high-level gameplay capabilities and does not replace per-frame combat, AI, physics, or animation state machines.

## Runtime State and Time

A lightweight Runtime State Coordinator aligns input, clocks, timers, presentation, and audio for Playing, Paused, Narrative, and Loading states. It is a policy coordinator, not another universal Game Mode state machine.

Gameplay uses scaled time; UI and essential presentation can use unscaled time. Timers declare their time domain and owner scope. Leaving a System, Manager, Window, Narrative Session, or Stage cancels its timers automatically. The framework exposes one timer model rather than competing implementations.

## Input Architecture

Unity Input System is the only player and UI input source. Product runtime code does not use `UnityEngine.Input`, legacy named axes, `StandaloneInputModule`, or direct device polling.

The game owns one canonical Input Action Asset containing at least:

- Gameplay;
- UI;
- Narrative;
- System;
- Debug.

`PlayerInput`, `InputSystemUIInputModule`, Naninovel, and runtime consumers use that asset. Composition owns the single EventSystem. Naninovel does not create another EventSystem or a private default action set.

Input contexts use scoped leases. An overlay may preserve selected Gameplay actions, while a modal choice or pause screen can suppress Gameplay and retain UI/Narrative actions. Releasing one lease never clears a lock held by another owner.

Continuous values such as movement and look are sampled through a read-only game-facing interface. Discrete actions use well-defined edges and callbacks. Gameplay systems own buffering, combo windows, and command consumption rules. A replaceable input source supports behavioral tests without introducing replay or networking requirements.

The supported experience includes keyboard/mouse, Xbox-style controllers, hot switching, device glyph changes, composite rebinding, conflict detection, fallback bindings, and persistent binding overrides. HOTAS remains a game-project extension until required.

## UI Architecture

The UI service owns window identity, prefab loading, layers, focus, and lifecycle. Callers request a Window by stable ID or type; they do not instantiate and string-register arbitrary objects before showing them.

UI ownership categories are:

- global windows that persist across Stages;
- Stage-owned windows that close on Stage exit;
- temporary modal layers managed as a focus/input stack.

Windows release only subscriptions they own. Event subscriptions return disposable handles and are collected by System, Manager, Window, and session scopes. The framework retains strongly typed events without requiring every consumer to manually pair registration and deregistration.

## Narrative Presentation

The Naninovel configuration and presentation rules in `narrative-module.md` apply. Narrative Action Setup additionally configures the verified Naninovel version, canonical input actions, unified settings, unified save contributor, test content, and validators.

Only two supported presentation modes exist:

- **Gameplay Overlay**: gameplay world rendering remains active; Naninovel World Camera is inactive; narrative UI appears in the Levity UI hierarchy.
- **Full Narrative Replace**: gameplay is suspended; a project-owned Narrative Camera Rig, Volume profile, and presentation state replace the gameplay rig until the session exits.

A Presentation Coordinator owns camera leases, UI Camera, URP Base/Overlay relationships, Canvas sorting, Volumes, post-processing, and the single active Audio Listener. Naninovel does not create default cameras, UI cameras, EventSystems, input modules, or private input actions.

## Independent Content Workspaces

Narrative and level content must be independently executable, not merely stored on separate Git branches.

### Narrative Workspace

Writers can select and continuously play any narrative route without loading a production Stage or Game Flow. The workspace provides the production Narrative UI and presentation host, editable fake game state, fake Gameplay Command handlers, save/load, locale and settings controls, and both presentation modes.

### Stage Workspace

Level designers can load and play any Stage without starting Naninovel or requiring completed `.nani` content. A Placeholder Narrative Backend displays the requested Sequence ID, parameters, and possible outcomes, then lets the tester complete, cancel, fail, wait, or select a typed outcome.

### Integration Workspace

Integration maps the same stable Sequence IDs from placeholder behavior to Naninovel implementations without changing Stage Flow. Validation confirms mappings, schemas, outcomes, resources, presentation policies, saves, and the final end-to-end task.

Stage Definitions and Narrative Sequence Definitions are separate assets rather than rows in one monolithic configuration. Generated registries and build lists avoid shared hand-edited merge hotspots.

## Persistence

Persistence is a public Core capability. Each module contributes versioned state through an asynchronous Save Contributor. Levity prepares, validates, and commits one Unified Save. Any contributor failure fails the operation and preserves the previous valid slot through temporary write and atomic replacement.

The save includes gameplay state, narrative state, settings, binding overrides, content source/version metadata, and committed Gameplay Command IDs. Load returns a typed result and never reports success after a contributor failure. Legacy filename APIs and fire-and-forget provider callbacks are deprecated.

## Resources, Pooling, and Mods

Gameplay and framework modules load through a narrow resource port using stable IDs, explicit errors, cancellation, and owner scope. Resources can remain the initial backend. Addressables and YooAsset are deferred because there is no hot-update requirement.

Object pooling is not part of resource loading. A single Toolkit pool may wrap Unity `ObjectPool<T>` when validated; projectile and VFX pools remain Stage/game-owned. Future Mod support may add a local content backend, but Mod loading, downloading, code sandboxing, and compatibility policy are out of current scope.

## Audio

The public audio surface uses stable Audio Event and Bus concepts rather than exposing a middleware API. Minimum supported behavior includes Master/Music/SFX/Voice/UI buses, settings synchronization, BGM crossfade, voice ducking, Stage-owned sound cleanup, and runtime diagnostics.

Before expanding the current audio implementation, a focused spike compares Unity Audio, FMOD, and Wwise using a parameter-driven engine loop, many fast 3D effects, communication voice ducking, and dynamic music. The result selects an adapter; the architecture does not preselect a winner.

## Diagnostics and Validation

A development diagnostics view exposes current Stage, Game Mode, Flow node, registered modules, input contexts and lease owners, time domains, Narrative Session, save availability, audio counts, and recent lifecycle errors.

Editor/build validation covers required actions, unique IDs, registered modes, scene references, Stage definitions, Narrative mappings, presentation configuration, EventSystem and Audio Listener ownership, old input API usage, unavailable dependencies, and incomplete generated setup.

## Reference Vertical Slice

The primary tracer bullet is:

```text
Narrative Action Setup
→ Main Menu
→ Mission Stage
→ keyboard/controller placeholder vehicle control
→ in-mission Gameplay Overlay dialogue
→ pause UI
→ Full Narrative Replace sequence and choice
→ result
→ Unified Save
→ load and restore the correct Stage, gameplay, and narrative state
```

The same delivery proves that Narrative Workspace can run the sequence without the Stage and Stage Workspace can run the mission with placeholder narrative.

## Migration

Migration is incremental. New Composition, Input, Stage, Narrative, UI, and Save contracts are introduced beside compatibility adapters. Old APIs are marked Deprecated with actionable migration diagnostics. Framework internals and samples migrate first; old entry points are removed only after no supported consumer remains. APIs with known correctness or data-integrity failures may be retired sooner.

## Out of Scope

- Multiplayer, server state, rollback networking, or local multiplayer.
- Hot updates and remote asset delivery.
- A complete Mod loader or code sandbox.
- Complete tactical, strategy, RPG, flight, or mech gameplay frameworks.
- Runtime narrative-backend switching.
- A framework-wide automatic dependency injection container.
- Exact arbitrary-frame restoration of cinematic presentation.
- Selecting an audio middleware without a representative spike.
