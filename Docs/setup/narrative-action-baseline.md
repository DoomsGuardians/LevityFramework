# Narrative Action Reference Baseline

Status: Implemented reference configuration for issue #24.

The repository is the concrete Narrative Action reference project. Apply the checked-in baseline from **Tools > Levity > Project Setup > Apply Narrative Action Baseline**, or in automation with:

```powershell
& $UnityEditor -batchmode -nographics -quit -projectPath $ProjectPath -executeMethod Levity.Editor.ProjectSetup.NarrativeActionProjectSetup.ApplyAndValidate -logFile $LogPath
```

The command is idempotent: it reuses the canonical assets, replaces the deterministic build-scene list, updates owned settings, and refuses ambiguous scene ownership instead of deleting an operator's duplicate objects.

## Project-owned taxonomy

| Path | Responsibility |
|---|---|
| `Assets/Levity/Runtime` | Engine-light runtime modules and contracts, organized by `Levity.*` assembly. |
| `Assets/Levity/Unity` | Application composition, Unity-facing services and systems, and package adapters. Existing global types remain source-compatible even though their folders now follow current responsibilities. |
| `Assets/Levity/Editor` | Project setup and authoring workspace tooling. |
| `Assets/Levity/Tests` | EditMode and PlayMode test assemblies. |
| `Assets/Content` | Project-authored narrative and scenario content. |
| `Assets/Scenes` | Bootstrap and playable scenes. |
| `Assets/Input` | Canonical Input System actions. |
| `Assets/Settings` | Project-owned render and generated input-reference assets. |

`Assets/Plugins`, `Assets/NaninovelData`, and `Assets/TextMesh Pro` retain vendor-owned layouts. Folder moves preserve existing `.meta` files and serialized GUIDs. Folder placement does not deprecate a type; only an explicit `[Obsolete]` API annotation represents a migration state.

## Ownership decisions

- `GameRoot` is the bootstrap scene and owns the single persistent EventSystem as a child.
- The EventSystem uses `InputSystemUIInputModule` and action references into `Assets/Input/Levity.inputactions`.
- `Levity.inputactions` is the only project input asset and contains `Gameplay`, `UI`, `Narrative`, `System`, and `Debug` maps.
- Player Settings selects the Input System package only. Naninovel neither spawns an EventSystem/input module nor processes legacy bindings; it consumes the same input asset.
- `NaninovelRuntimePlayer` remains the runtime initialization owner. Naninovel automatic initialization is disabled and engine objects are scene-independent.
- Main Menu owns one authored Camera and Audio Listener. Naninovel's additional UI camera is disabled.
- the checked-in URP asset is assigned in Graphics Settings and every quality level.
- build order is `GameRoot` followed by `MainMenu`.
- Unified Save and Stage compatibility are composed through `GameRoot`; their domain implementations remain in their `Levity.*` assemblies.

## Preset comparison checklist

| Artifact or setting | Owner | Idempotency behavior | Minimal | Narrative Action |
|---|---|---|:---:|:---:|
| Bootstrap and Main Menu build scenes | Project setup | Replaces list in canonical order | Bootstrap only | Yes |
| `Levity.inputactions` and UI references | Project setup | Reuses stable assets and updates references | Core UI/System maps | Yes, all five maps |
| One EventSystem with Input System module | Bootstrap scene | Creates if absent; rejects duplicates | Yes | Yes |
| Main Menu camera and Audio Listener | Main Menu scene | Validates exactly one of each | Optional empty-loop camera | Yes |
| URP asset on Graphics and quality levels | Project settings | Reassigns every level | Yes | Yes |
| Player identity and Input System-only mode | Project settings | Reapplies owned values | Yes | Yes |
| Stage compatibility and Unified Save | Application composition | Validates checked-in composition | No | Yes |
| Naninovel configuration and adapter | Naninovel adapter | Reapplies ownership flags and input reference | No | Yes |
| Narrative content and workspaces | Project content/editor | Checked in; validation is non-generative | No | Yes |

Issue #22 can use this table to compare installer, template, package sample, or wizard delivery without rediscovering which artifacts constitute the working baseline.
