# TASK: L01_02_PREFAB

Full name:

```text
LIVE01_02_CREATE_PLAYER_PREFAB_AND_MINIMAL_LIVE_TEST_SCENE
```

## Objective

Create the live player prefab and the smallest manual test scene needed for later spawn/run wiring.

This task composes the body that can be placed in a scene. It must not start a generated run, consume spawn requests, connect route or camera transitions, bind HUD, create PlayMode tests, edit build settings, or change pure Character/MAP contracts.

## Entry Gate

Verify:

```text
Current Task: CLI/MCP/TASKS/L01_02.md
L01_01 RESULT exists
L01_01 RESULT sha256: 4e269dddc9d49ab8a146dd4fe759b2471c516229d7828a44f84541ab3a41c983
L01_01 RESULT contains STATUS: PASS
L01_01 RESULT contains None. Input provider only.
L01_01 RESULT contains Input actions asset only. No scene or prefab wiring.
Assets/_Game/Live/Input/CharacterLiveControls.inputactions exists
Assets/_Game/Live/Runtime/Input/CharacterLiveInputSource.cs exists
CLI/MCP/INPUTS/LIVE_LOCK.md exists
CLI/MCP/INPUTS/LIVE_LOCK.md contains LOCK_STATE: FILLED_BY_L00_02
L01_03 and later tasks are locked
```

If false, write `STATUS: BLOCKED`.

## Read

Read in order:

1. `CLI/MCP/ENTRY.md`
2. `CLI/MCP/RULES.md`
3. `CLI/MCP/STATUS.md`
4. `CLI/MCP/MASTER.md`
5. `CLI/MCP/INPUTS/LIVE_SRC.md`
6. `CLI/MCP/INPUTS/LIVE_LOCK.md`
7. `CLI/MCP/REPORTS/L00_02_RESULT.md`
8. `CLI/MCP/REPORTS/L01_01_RESULT.md`
9. `CharacterDesign/MCP/REPORTS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT_RESULT.md`
10. `CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`
11. `Assets/_Game/Character/Runtime/**`
12. `Assets/_Game/Live/Runtime/**`
13. `Assets/_Game/Live/Input/CharacterLiveControls.inputactions`
14. `Assets/_Game/Map/Runtime/**`
15. `Assets/_Game/Scenes/**`
16. `Assets/**.prefab`
17. `Packages/manifest.json`
18. `ProjectSettings/EditorBuildSettings.asset`

Use search before opening broad trees. Read existing live input APIs before writing any prefab references.

## Allowed Writes

```text
Assets/_Game/Live/Runtime/**
Assets/_Game/Live/Prefabs/**
Assets/_Game/Scenes/Live/**
CLI/MCP/REPORTS/L01_02_RESULT.md
```

Forbidden writes:

```text
Assets/_Game/Character/Runtime/**
Assets/_Game/Map/Runtime/**
Assets/_Game/Live/Input/**
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

Create a live player prefab and minimal live test scene.

Required prefab:

```text
Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab
```

Prefab requirements:

```text
Root GameObject name: CharacterLivePlayer
Rigidbody2D present
Rigidbody2D bodyType: Kinematic
Rigidbody2D gravityScale: 0
Collider2D present and sized from the existing Character collider spec or locked live config
CharacterLiveInputSource present and assigned to CharacterLiveControls.inputactions
Live player rig/binding component present
No Animator requirement
No AudioSource
No UI components
No MAP generator component
No SceneManager usage
No spawn request consumer
No route or camera transition consumer
```

Required scene:

```text
Assets/_Game/Scenes/Live/CharacterLiveTest.unity
```

Scene requirements:

```text
Contains one instance of CharacterLivePlayer prefab
Contains one orthographic camera for manual inspection
May contain minimal static floor/collider only if needed for safe manual inspection
Must not be added to EditorBuildSettings in this task
Must not include generated MAP bootstrap yet
Must not include HUD, audio, save, enemy, item, or transition systems
```

Required runtime behavior:

```text
Add a live player rig/binding API that L01_03 can use to find Rigidbody2D, Collider2D, CharacterLiveInputSource, and consume fixed input snapshots.
Do not move the Rigidbody2D in this task unless the existing live rig only performs deterministic no-op validation.
Do not consume Character spawn, route, room transition, bomb, rope, damage, death, run failure, HUD, or presentation requests.
Do not add or modify CharacterActionId values.
Do not rewrite pure Character movement/combat/survival policies.
Do not duplicate MAP cell constants or generated room logic.
```

Recommended files, unless repo context proves a better local pattern:

```text
Assets/_Game/Live/Runtime/Player/CharacterLivePlayerRig.cs
Assets/_Game/Live/Runtime/Player/CharacterLivePlayerRigValidator.cs
Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab
Assets/_Game/Scenes/Live/CharacterLiveTest.unity
```

## Required Verification

Run or report the nearest available equivalents:

```text
Unity compile: PASS required
Character EditMode baseline: 177/177 PASS required unless the project runner is unavailable
Prefab audit: required components present and forbidden components absent
Scene audit: scene contains prefab instance and camera, no forbidden systems
Input asset audit: L01_01 bindings unchanged, no E/F/Q
Forbidden feature audit: no basic attack, melee, shoot, dash, wall jump, double jump, or new ActionId
Scope audit: changed files only in allowed paths plus result
```

MAP EditMode 13,536 may remain anchored unless local policy requires rerun, because this task must not touch MAP runtime.

If Unity cannot run, write `STATUS: BLOCKED` unless the MCP rules explicitly allow an environment-only substitute.

## Required Report

Write:

```text
CLI/MCP/REPORTS/L01_02_RESULT.md
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
None. Prefab and manual scene composition only.
```

For this task, `ASSETS_WIRED` must include:

```text
CharacterLiveControls.inputactions -> CharacterLiveInputSource -> CharacterLivePlayer prefab
CharacterLivePlayer prefab -> CharacterLiveTest scene instance
No generated run spawn wiring
```

## Completion

PASS requires:

```text
Live player prefab created
Minimal live test scene created
Input source wired to prefab
Compile clean or environment-approved equivalent
Character baseline preserved
No scene build-settings change
No generated run/spawn/route/HUD/MAP adapter/request consumer changes
```

If PASS:

```text
Finalize L01_02 as COMPLETE.
Current Task after finalize: NONE
Do not auto-open L01_03_SPAWN.
```
