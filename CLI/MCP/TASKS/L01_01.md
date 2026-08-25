# TASK: L01_01_INPUT

Full name:

```text
LIVE01_01_IMPLEMENT_KEYBOARD_INPUT_TO_LOCKED_CHARACTER_ACTIONS
```

## Objective

Implement the first live integration surface: keyboard input captured through Unity Input System and converted into the completed Character runtime input contracts.

This task must not make the player playable yet. Do not create or edit scenes, prefabs, spawn consumers, camera consumers, HUD, MAP adapters, save data, audio, animation, or build settings.

## Entry Gate

Verify:

```text
Current Task: CLI/MCP/TASKS/L01_01.md
L00_02 RESULT exists
L00_02 RESULT sha256: bc4b91f03d9177fc41044430081374d0f9c3f575aefeba9bab7d302f16ac24a4
L00_02 RESULT contains STATUS: PASS
L00_02 RESULT contains LOCK_STATE: FILLED_BY_L00_02
CLI/MCP/INPUTS/LIVE_LOCK.md exists
CLI/MCP/INPUTS/LIVE_LOCK.md contains LOCK_STATE: FILLED_BY_L00_02
L01_02 and later tasks are locked
```

If false, write `STATUS: BLOCKED`.

## Read

Read in order:

1. `CLI/MCP/ENTRY.md`
2. `CLI/MCP/RULES.md`
3. `CLI/MCP/STATUS.md`
4. `CLI/MCP/MASTER.md`
5. `CLI/MCP/INPUTS/CHAR_EXIT.md`
6. `CLI/MCP/INPUTS/LIVE_SRC.md`
7. `CLI/MCP/INPUTS/LIVE_LOCK.md`
8. `CLI/MCP/REPORTS/L00_01_RESULT.md`
9. `CLI/MCP/REPORTS/L00_02_RESULT.md`
10. `CharacterDesign/MCP/REPORTS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT_RESULT.md`
11. `CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`
12. `Assets/_Game/Character/Runtime/**`
13. `Assets/_Game/Map/Runtime/**`
14. `Assets/_Game/Tests/EditMode/Character/**`
15. `Assets/_Game/Tests/EditMode/Map/**`
16. `Assets/**.inputactions`
17. `Packages/manifest.json`

Use search before opening broad trees. Prefer existing Character constructors/factories over guessed APIs.

## Allowed Writes

```text
Assets/_Game/Live/Runtime/**
Assets/_Game/Live/Input/**
CLI/MCP/REPORTS/L01_01_RESULT.md
```

The `Assets/_Game/Live/Runtime/**` allowance includes one new live assembly definition:

```text
Assets/_Game/Live/Runtime/Game.Character.Live.asmdef
```

Forbidden writes:

```text
Assets/_Game/Character/Runtime/**
Assets/_Game/Map/Runtime/**
Assets/_Game/Live/Prefabs/**
Assets/_Game/Scenes/**
Assets/_Game/Tests/**
Packages/**
ProjectSettings/**
MapDesign/**
CharacterDesign/**
CLI/MCP/STATUS.md
CLI/MCP/MASTER.md
CLI/MCP/TASKS/**
Builds/**
Temp/**
```

## Required Implementation

Create the live input layer only.

Required asset:

```text
Assets/_Game/Live/Input/CharacterLiveControls.inputactions
```

Required action map:

```text
Player
```

Required actions and bindings:

```text
Move: Value/Axis or Vector2 horizontal, A/D and Left/Right
Down: Button, S and DownArrow
Jump: Button, Space
Action: Button, X
Bomb: Button, Z
Rope: Button, C
```

Legacy `E/F/Q` bindings are read-only precedent and must not be reused.

Required runtime behavior:

```text
Use Unity Input System, not legacy Input.GetKey polling.
Convert device input into existing Character runtime input snapshot/buffer contracts.
Do not add new CharacterActionId values.
Do not rename or rewrite existing Character runtime public APIs.
Move and Down are axis/state inputs, not new CharacterActionId values.
Map Jump, Action, Bomb, Rope to the existing action IDs.
Map Down + Action to existing SafeDrop semantics if the Character runtime exposes SafeDrop; otherwise preserve Down direction plus Action exactly as the existing contract expects.
Preserve pressedThisFrame/releasedThisFrame edges collected in Update until a fixed-step consumer reads them.
Keep held states continuous across frames.
Do not move Rigidbody2D, teleport, spawn, despawn, damage, spend inventory, or consume gameplay requests.
Expose a clear public live input provider/adapter API for L01_02 prefab wiring.
```

Recommended files, unless repo context proves a better local pattern:

```text
Assets/_Game/Live/Runtime/Game.Character.Live.asmdef
Assets/_Game/Live/Runtime/Input/CharacterLiveInputSource.cs
Assets/_Game/Live/Runtime/Input/CharacterLiveInputAdapter.cs
Assets/_Game/Live/Runtime/Input/CharacterLiveInputState.cs
Assets/_Game/Live/Input/CharacterLiveControls.inputactions
```

The assembly may reference:

```text
Game.Character.Runtime
Unity.InputSystem
UnityEngine
```

Do not add a MAP reference in this task unless the existing input contract directly requires it. If such a reference is required, write `STATUS: BLOCKED` and explain why.

## Required Verification

Run or report the nearest available equivalents:

```text
Unity compile: PASS required
Character EditMode baseline: 177/177 PASS required unless the project runner is unavailable
MAP EditMode baseline: record existing 13,536 PASS anchor; rerun only if local policy requires it
Input asset audit: Player map and six actions/bindings present
Forbidden binding audit: no E/F/Q in new live input asset
Forbidden feature audit: no basic attack, melee, shoot, dash, wall jump, double jump, or new ActionId
Scope audit: changed files only in allowed paths plus result
```

If Unity cannot run, write `STATUS: BLOCKED` unless compile/test unavailability is an environment-only issue already allowed by the project MCP rules.

## Required Report

Write:

```text
CLI/MCP/REPORTS/L01_01_RESULT.md
```

Include the common locked sections:

```text
TASK
STATUS
SUMMARY
READ
CHANGED
CREATED
TESTS
BUILD
SCOPE_VALIDATION
FORBIDDEN_AUDIT
NEXT
```

Include the implementation sections:

```text
LIVE_CONTRACTS_USED
REQUESTS_CONSUMED
ASSETS_WIRED
MANUAL_VERIFICATION
REGRESSION_BASELINE
```

For this task, `REQUESTS_CONSUMED` must say:

```text
None. Input provider only.
```

For this task, `ASSETS_WIRED` must say:

```text
Input actions asset only. No scene or prefab wiring.
```

## Completion

PASS requires:

```text
Live input asset created
Live input adapter/provider created
Compile clean or environment-approved equivalent
Character baseline preserved
No forbidden bindings
No new ActionId values
No scene/prefab/HUD/spawn/MAP adapter changes
```

If PASS:

```text
Finalize L01_01 as COMPLETE.
Current Task after finalize: NONE
Do not auto-open L01_02_PREFAB.
```
