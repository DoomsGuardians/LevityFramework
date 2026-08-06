# PRD: Levity Framework Usage and Narrative Integration

> [!NOTE]
> 本文件是已发布规格的本地快照。需求状态、讨论和实施拆分以 [GitHub Issue #1](https://github.com/DoomsGuardians/LevityFramework/issues/1) 为准；不要在本文件维护进度。

## Problem Statement

LevityFramework currently integrates Naninovel as a broad service that coordinates scripts, input, cameras, saves, settings, and stage behavior. Naninovel is capable enough to satisfy the game's dialogue, choice, branching, localization, and presentation needs, and its `.nani` format is a productive authoring tool. However, exposing Naninovel concepts throughout game flow and gameplay would make the framework's architecture, persistence, settings, and tests depend on one third-party product.

The user needs Naninovel to become the default implementation of a first-class Levity narrative module: fully capable for current production, integrated with level Flow, unified with gameplay saves and settings, and isolated behind stable game-owned contracts. There is no current requirement to replace Naninovel or invent another narrative language.

More broadly, Levity's current repository mixes the responsibilities of a reusable framework, a project template, and a source toolkit. Bootstrap, input, UI, Stage configuration, service access, persistence, optional integrations, and samples do not yet form one reliable adoption path. Several APIs described by documentation differ from their implementations, some configuration fields are not consumed, old and new input paths coexist, and narrative or level content cannot currently be produced and played independently.

The user needs an opinionated but lightweight framework for PC single-player games. Its primary proving ground is a narrative-driven mission action game, while the same Core must remain fast to adopt for Game Jam tactical, strategy, and RPG projects.

## Solution

Introduce a backend-agnostic narrative module owned by Levity. Game Flow starts validated narrative sequences and reacts to typed outcomes, while Naninovel executes the `.nani` content inside each sequence. A dedicated adapter maps sequence IDs, state snapshots, settings, custom gameplay commands, failures, and diagnostics between Levity and Naninovel.

Levity coordinates a unified, atomic save across gameplay and narrative state. Ordinary dialogue and waiting choices remain saveable; complex cinematic presentation can block saving with an explicit reason. Levity also owns the canonical settings model and synchronizes relevant values into Naninovel.

The design is delivered as an internal Unity Package with explicit Composition, stable lifecycle and Stage contracts, a canonical Input System setup, unified UI/events/resources/persistence, isolated narrative modules, and editor Setup/validation. A runnable starter and independent Narrative, Stage, and Integration workspaces prove that the public interfaces support real production workflows.

## User Stories

1. As a narrative designer, I want to continue authoring dialogue, choices, branching, and presentation in `.nani`, so that I retain Naninovel's mature workflow.
2. As a level designer, I want a Flow node to play a named narrative sequence, so that I can place narrative at specific gameplay beats.
3. As a level designer, I want the Flow node to wait for narrative completion, so that subsequent gameplay starts at the intended time.
4. As a level designer, I want narrative sequences to return typed outcomes, so that choices can drive gameplay and stage branches safely.
5. As a level designer, I want sequence inputs and outcomes validated in the editor, so that broken content is found before runtime.
6. As a level designer, I want stable sequence identifiers, so that moving or renaming `.nani` assets does not silently break Flow data.
7. As a content author, I want dialogue-only branches to remain in `.nani`, so that narrative detail is not duplicated in the level Flow editor.
8. As a gameplay designer, I want game-affecting branches to be handled by Flow, so that quests, stages, and gameplay state remain game-owned.
9. As a gameplay programmer, I want narrative scripts to invoke registered gameplay commands, so that scripts can affect the game without accessing global services.
10. As a gameplay programmer, I want state-changing narrative commands to have stable execution identities, so that loading a save cannot apply an effect twice.
11. As a gameplay programmer, I want commands classified by behavioral effect, so that persistence and presentation safety policies can be enforced consistently.
12. As a player, I want to save during ordinary dialogue, so that I can stop without losing narrative progress.
13. As a player, I want to save while waiting at a choice, so that loading restores the same available options.
14. As a player, I want saving disabled with a clear reason during an unsafe cinematic, so that saving does not unexpectedly skip or corrupt presentation.
15. As a player, I want gameplay and narrative to restore from one save slot, so that they can never disagree about current progress.
16. As a player, I want a failed save to preserve my previous valid save, so that partial state cannot destroy progress.
17. As a player, I want narrative volume changes to apply immediately, so that settings feel responsive.
18. As a player, I want text speed changes to follow Naninovel's predictable print behavior, so that dialogue remains stable.
19. As a player, I want changing language to refresh the current dialogue and choices without replaying gameplay effects, so that localization is safe during a session.
20. As a game programmer, I want only one narrative session active by default, so that overlapping scripts cannot fight over input, camera, or UI.
21. As a game programmer, I want explicit wait, replace, and cancel policies for concurrent requests, so that interruptions are intentional.
22. As a game programmer, I want structured completed, cancelled, and failed results, so that Flow handles every terminal state explicitly.
23. As a game programmer, I want scene transitions to declare whether a narrative must finish, persist, or cancel, so that lifecycle behavior is not accidental.
24. As a game programmer, I want Flow tests to run against a fake narrative backend, so that most behavior can be verified without initializing Naninovel.
25. As an integration programmer, I want Naninovel-specific capabilities isolated in an explicit extension surface, so that uncommon features do not pollute the core contract.
26. As an integration programmer, I want custom commands to honor asynchronous completion, so that saving cannot hang indefinitely.
27. As a maintainer, I want compile-time assembly boundaries, so that backend dependencies cannot leak into Core or Flow unnoticed.
28. As a maintainer, I want the framework to compile without Naninovel installed, so that the narrative core remains reusable.
29. As a maintainer, I want content that selects an unavailable backend to fail validation clearly, so that a production build cannot silently skip narrative.
30. As a maintainer, I want structured session diagnostics, so that failures can be traced from a Flow node to the mapped script and state.
31. As a content author, I want to navigate from a Flow node to its `.nani` source, so that iteration is fast.
32. As a maintainer, I want existing Naninovel integration fields to migrate through a compatibility layer, so that the architecture can change incrementally.
33. As a framework user, I want the current game to select Naninovel as a required backend while the framework keeps it optional, so that reuse does not require runtime hot-swapping.
34. As a product owner, I want the first delivery to prove one end-to-end narrative and gameplay path, so that architecture risk is retired before broad feature work.
35. As a solo developer, I want to create a runnable project from a preset in under 30 minutes, so that the framework remains useful during Game Jams.
36. As a framework user, I want a Minimal preset and a Narrative Action preset, so that unrelated configuration does not block a project from starting.
37. As a framework user, I want one explicit Composition entry point, so that I never modify framework Core to register game modules.
38. As a programmer, I want Service, System, Manager, and Game Mode to have strict ownership rules, so that I know where new behavior belongs.
39. As a programmer, I want a Manager to be released automatically with its Stage, so that old scene objects cannot receive new-stage calls.
40. As a programmer, I want Stage changes to complete as one asynchronous transaction, so that a failed load cannot leave the application half initialized.
41. As a designer, I want Stage configuration to contain only fields consumed by runtime behavior, so that the Inspector does not advertise false capabilities.
42. As a programmer, I want strong Stage, Mode, Window, Sequence, and Audio IDs, so that unrelated identifiers cannot be mixed or silently broken by renames.
43. As a player, I want all gameplay and UI input to use Unity Input System, so that keyboard and controller behavior is consistent.
44. As a player, I want complete runtime rebinding, conflict detection, device glyph updates, and persisted bindings, so that controls fit my hardware and preferences.
45. As a UI programmer, I want exactly one EventSystem and UI input module, so that Naninovel and gameplay UI cannot compete for navigation or pointer focus.
46. As a gameplay programmer, I want scoped input contexts, so that pause menus, choices, loading, and task dialogue suppress only the actions they own.
47. As a gameplay programmer, I want continuous and discrete input exposed with clear semantics, so that action buffering remains a gameplay decision.
48. As a programmer, I want a replaceable input source, so that combat behavior can be tested without real devices.
49. As a player, I want pause, loading, narrative, UI, timers, and audio to share one runtime-state policy, so that one module cannot accidentally resume another.
50. As a programmer, I want timers owned by lifecycle scopes and time domains, so that callbacks cannot outlive their Stage or Window.
51. As a UI author, I want windows loaded and owned by the UI service, so that showing a window does not require manual construction and string registration.
52. As a programmer, I want event subscriptions released with their owner, so that scene transitions do not create duplicate or ghost callbacks.
53. As a writer, I want to author and continuously play all narrative content without a production level or Stage Flow, so that writing and presentation testing are independent.
54. As a writer, I want fake game state and Gameplay Commands in the Narrative Workspace, so that every narrative branch can be tested before gameplay exists.
55. As a level designer, I want to load and play any Stage without Naninovel or finished scripts, so that level production is not blocked by writing progress.
56. As a level designer, I want placeholder narrative nodes to expose possible typed outcomes, so that all gameplay branches can be tested independently.
57. As an integrator, I want placeholder and Naninovel backends to share the same Sequence IDs, so that integration does not rewrite Stage Flow.
58. As a narrative designer, I want Gameplay Overlay dialogue to preserve the gameplay camera, so that mission communications do not duplicate world rendering.
59. As a narrative designer, I want Full Narrative Replace to switch camera, post-processing, input, and audio ownership explicitly, so that complete visual-novel scenes are predictable.
60. As a UI author, I want Naninovel UI hosted in Levity's UI hierarchy, so that sorting, focus, cameras, and settings follow one policy.
61. As a developer, I want setup validation to reject legacy input, duplicate EventSystems, invalid camera stacks, and multiple listener ownership, so that integration errors are found before playtesting.
62. As a Game Jam developer, I want optional Toolkit and Experimental capabilities available without making them mandatory architecture, so that useful prior-project code remains accessible.
63. As a maintainer, I want every capability labeled Core, Supported, Toolkit, Experimental, or Deprecated, so that source-code presence is not mistaken for production support.
64. As a maintainer, I want old APIs migrated incrementally with actionable diagnostics, so that the framework remains runnable throughout the redesign.
65. As a developer, I want resources loaded through stable IDs and a narrow port, so that a future local Mod backend does not require rewriting game logic.
66. As an audio designer, I want a representative middleware comparison before the framework commits to Unity Audio, FMOD, or Wwise, so that the selected workflow matches the game.
67. As a developer, I want a diagnostics view of lifecycle, input, Flow, narrative, save, and audio ownership, so that hidden runtime state is inspectable.
68. As a product owner, I want one playable vertical slice to exercise setup, mission control, dialogue, pause, full narrative, results, save, and restore, so that framework usability is demonstrated end to end.

## Implementation Decisions

- Build four deep modules: Narrative Core, Narrative Flow, the Naninovel adapter, and Narrative Editor tooling.
- Narrative Core owns the small, stable interface for sequence execution, session lifecycle, typed outcomes, save availability, settings mapping, and backend snapshots.
- Narrative Flow owns bridge-scale orchestration and never models individual dialogue lines, choices, or cinematic commands.
- The Naninovel adapter is the default backend and the only normal integration layer that references Naninovel types.
- `.nani` remains the source authoring format. No Levity narrative language or intermediate content format is introduced.
- A sequence registry maps stable narrative sequence IDs to backend scripts and optional entry points.
- Each sequence defines a validated input and outcome schema. Runtime serialization may use generic data, but editor authoring remains typed and validated.
- Only one session is active by default. A second request must explicitly wait, replace, or cancel instead of relying on implicit queuing or interruption.
- Sessions terminate as completed, cancelled, or failed and may carry a typed business outcome.
- Flow owns branches that affect gameplay, quests, stages, and scene routing. `.nani` owns branches internal to dialogue and presentation.
- Gameplay commands are registered through narrow game-owned ports and never locate concrete services through global state.
- Commands are classified as pure, stateful, irreversible, or presentation commands. Stateful executions receive stable IDs recorded in the save.
- Levity owns unified-save orchestration. Gameplay and Naninovel contribute compatible, versioned state to one atomic commit.
- A failed contributor fails the entire save and preserves the prior valid slot.
- Save availability is aggregated across active modules. Ordinary dialogue and choices allow saving; unsafe cinematic presentation blocks it with a reason.
- The design accepts Naninovel's command-level restore semantics rather than promising character-position or animation-frame snapshots.
- Levity owns the canonical settings model. The adapter applies audio, text speed, locale, and other narrative settings using Naninovel's existing runtime behavior.
- Scene transitions explicitly complete, persist, or cancel the active session; completion before transition is the default.
- Missing scripts, invalid parameters, unavailable backends, and initialization errors return structured failures and detailed diagnostics rather than being silently skipped.
- Core, Flow, Adapter, and Editor dependency directions are enforced with Unity assembly definitions.
- The current application composition root selects the narrative backend through the backend-neutral contract.
- Existing Naninovel service and stage configuration entry points are wrapped, deprecated, and migrated incrementally.
- Runtime backend hot-swapping is not supported. Naninovel is optional to the reusable framework but required by a game that selects it.
- The first tracer bullet plays one sequence from Flow, receives a choice outcome, enters a gameplay branch, saves and restores dialogue or choice state without repeating a gameplay side effect, and synchronizes settings.
- Distribute Levity as an internal Unity Package with separate starter/sample content rather than requiring users to copy and edit Core.
- Provide Minimal and Narrative Action presets through an idempotent editor Setup tool that creates visible, game-owned assets and previews changes.
- Classify capabilities as Core, Supported Module, Toolkit, Experimental, or Deprecated; unused utilities may remain without being represented as equally mature public architecture.
- Keep the names Service, System, Manager, and Game Mode, while defining Manager as strictly Stage/Scene-owned.
- Use explicit Composition and deterministic Register, Initialize, and Start phases instead of parallel service-locator access patterns.
- Replace fixed configuration paths and closed framework enums with explicit registries and distinct stable ID types.
- Make Stage changes single-flight asynchronous transactions with validation, loading leases, complete lifecycle ordering, and typed terminal results.
- Coordinate Playing, Paused, Narrative, and Loading policies through a lightweight runtime-state coordinator.
- Use Unity Input System exclusively. One game-owned action asset supplies Gameplay, UI, Narrative, System, and Debug maps to PlayerInput, the single UI module, and Naninovel.
- Support keyboard/mouse and controller hot switching, full rebinding, conflict detection, glyph updates, binding persistence, scoped input contexts, and a test input source.
- Make UI Service responsible for window construction, stable identity, layers, focus, and ownership. Event subscriptions return disposable handles collected by lifecycle scopes.
- Keep one timer API with explicit scaled or unscaled time and owner-scope cancellation.
- Host Naninovel UI inside Levity's UI hierarchy and prohibit Naninovel-created default cameras, UI cameras, EventSystems, input modules, and input assets.
- Support only Gameplay Overlay and Full Narrative Replace presentation modes, coordinated through explicit camera, URP stack, Volume, post-processing, UI, and Audio Listener leases.
- Provide an independent Narrative Workspace with production presentation plus fake gameplay state and commands.
- Provide an independent Stage Workspace with a placeholder narrative backend and tester-selected outcomes.
- Provide an Integration Workspace that binds the same stable Sequence IDs to Naninovel implementations and validates the combined experience.
- Split Stage and Narrative definitions into individually owned assets and generate shared registries rather than hand-editing monolithic configuration.
- Keep Resources as the initial asset backend behind a stable resource port. Defer Addressables, YooAsset, and Mod loading until concrete requirements exist.
- Keep object pooling separate from resource loading. Toolkit pooling should converge on one validated API rather than competing implementations.
- Define middleware-neutral Audio Event and Bus contracts, then select Unity Audio, FMOD, or Wwise after a representative technical spike.
- Migrate incrementally through compatibility adapters and actionable deprecation warnings; APIs with correctness or data-integrity failures may be retired sooner.

## Testing Decisions

- Tests assert public behavior and persisted outcomes rather than internal adapter calls or private state.
- Narrative Core contract tests cover session lifecycle, concurrency policies, cancellation, structured failures, typed outcomes, and save availability.
- Narrative Flow tests use a fake backend to cover sequence requests, result branches, scene-transition policies, and backend failures without starting Naninovel.
- Naninovel adapter integration tests cover script lookup and playback, waiting choices, typed outcomes, settings mapping, localization refresh, and initialization failure.
- Unified-save round-trip tests cover command-level narrative position, choice state, gameplay state, settings, and committed gameplay-command IDs.
- Save failure tests inject a failing contributor and prove that the previous valid save remains loadable.
- Gameplay-command tests prove that restored sessions do not repeat committed stateful effects.
- Presentation-policy tests prove that ordinary dialogue is saveable and unsafe cinematics report a useful blocked reason.
- Editor tests cover unknown sequence IDs, missing scripts or entry points, invalid input types, unhandled outcomes, missing resources, and unavailable backend packages.
- Existing lifecycle and service tests, where available, provide conventions; new tests should not depend on the global service locator when a fake port is sufficient.
- The tracer bullet is the first integration fixture and becomes the regression scenario for future backend and Flow changes.
- Composition tests verify missing dependencies fail before Start and shutdown follows ownership order.
- Stage transaction tests verify validation, cancellation, failure recovery, single-flight behavior, Manager cleanup, and Ready-only completion.
- Input tests verify New Input System-only behavior, action-map leases, keyboard/controller switching, rebinding persistence, UI focus, and absence of input leakage during pause, loading, and narrative.
- UI tests verify window ownership, layer focus, modal behavior, and automatic subscription cleanup.
- Runtime-state tests verify gameplay time, unscaled presentation time, timer scope cancellation, and independent lease ownership.
- Presentation tests verify Gameplay Overlay never enables a second world camera and Full Narrative Replace restores camera, Volume, UI, input, and Audio Listener state.
- Narrative Workspace tests prove complete routes can run with fake game state and commands and no production Stage.
- Stage Workspace tests prove Stages can run with placeholder narrative and no Naninovel content.
- Integration tests prove the same Sequence ID works first with placeholder outcomes and then with the real Naninovel backend without Flow changes.
- Setup tests verify idempotence, preservation of user changes, generated defaults, migration diagnostics, and preset-specific requirements.
- Validation tests reject duplicate IDs, missing actions, legacy input use, invalid Stage/Sequence mappings, duplicate EventSystems, invalid URP camera relationships, and incomplete save contributors.
- Each implementation cycle should be a vertical red-to-green slice through a confirmed public seam; tests should observe caller-visible behavior and avoid mocks of internal framework collaborators.

## Out of Scope

- Replacing, forking, or reimplementing Naninovel.
- Building a Levity-specific narrative scripting language, parser, or full narrative editor.
- Runtime switching between multiple narrative backends.
- Fine-grained Flow nodes for individual dialogue lines, choices, camera operations, or presentation commands.
- A framework-wide replacement of the current composition root or service locator.
- Exact restoration of arbitrary animation, audio, camera, or text-reveal frames during a cinematic.
- Supporting every legacy roadmap feature in the first implementation slice.
- Permanent support for both legacy and new integration APIs after migration completes.
- Public third-party distribution and Naninovel license policy; this requires a separate decision if distribution scope expands.
- Multiplayer, network synchronization, rollback networking, and local multiplayer.
- Hot updates, remote asset delivery, a complete Mod loader, or code sandboxing.
- Complete tactical, strategy, RPG, flight, mech, AI, weapon, or mission gameplay frameworks.
- Selecting an audio middleware before the representative spike.
- Treating every retained Toolkit or Experimental helper as stable production API.
- Maintaining multiple EventSystems, input backends, timer models, or service-location mechanisms as supported alternatives.

## Further Notes

- The installed Naninovel version saves command-level playback state and choice state, and completes active asynchronous commands before serialization. The Levity save policy must account for that behavior.
- The legacy Naninovel integration roadmap is a useful catalog of potential capabilities but is not the architectural authority for module boundaries.
- Naninovel replacement is an option preserved by the boundary, not a roadmap commitment.
- The first implementation should remain a vertical tracer bullet rather than a broad rewrite of existing integrations.
- The framework-wide architectural context is recorded in `Docs/architecture/framework-usage.md`; the narrative-specific architecture remains in `Docs/architecture/narrative-module.md`.
- The primary proving loop is main menu to base/hangar, mission briefing, real-time task, in-mission narrative, result, post-mission narrative, progression save, and return to base.
- Minimal and Narrative Action presets share one Core; Jam and Production modes differ in validation strictness rather than runtime semantics.
