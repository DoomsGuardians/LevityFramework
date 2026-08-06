# Levity Framework

Levity Framework is a reusable Unity game framework that coordinates gameplay, narrative, presentation, persistence, and settings without making game logic depend on a specific third-party implementation.

## Language

**Narrative Sequence（叙事桥段）**:
A named, authored unit of narrative that runs to a meaningful completion and may return a business result to the game flow.
_Avoid_: Dialogue task, Naninovel script, cutscene

**Game Flow（游戏流程）**:
The orchestration of gameplay and narrative sequences at the level of stages, conditions, and business outcomes.
_Avoid_: Narrative script, dialogue flow

**Narrative Session（叙事会话）**:
One active execution of a narrative sequence, including its input, outcome, lifecycle, and save availability.
_Avoid_: Dialogue instance, script player

**Narrative Outcome（叙事结果）**:
A typed result returned by a narrative sequence that the game flow can use to choose its next branch.
_Avoid_: Naninovel variable, string event

**Narrative Backend（叙事后端）**:
An implementation that executes narrative sequences and captures or restores its own narrative state. Naninovel is the default backend.
_Avoid_: Narrative owner, game flow

**Gameplay Command（玩法命令）**:
A controlled request from a narrative sequence to inspect or change gameplay state through a registered game-owned capability.
_Avoid_: Global event, direct service access

**Save Availability（保存许可）**:
The combined statement from active game modules that saving is allowed or blocked with a reason at the current moment.
_Avoid_: Save button state, Naninovel save lock

**Safe Presentation State（安全演出状态）**:
A presentation state whose narrative and gameplay meaning can be restored without reproducing an in-progress cinematic effect.
_Avoid_: Exact animation frame, arbitrary snapshot

**Unified Save（统一存档）**:
One committed save containing compatible gameplay, narrative, settings, and execution-progress state that succeeds or fails as a whole.
_Avoid_: Naninovel save, provider fragment
