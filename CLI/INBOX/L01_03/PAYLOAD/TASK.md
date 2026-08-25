# TASK: L01_03_SPAWN

Full name:

```text
LIVE01_03_CONSUME_PLAYER_SPAWN_REQUEST_AND_START_LIVE_RUN
```

## Objective

Connect the live player prefab to the completed Character spawn and movement contracts so the manual live scene can start a run and accept keyboard movement.

This task owns only the first live run loop: one spawn request consumption, run session initialization, and FixedUpdate input-to-movement application using existing Character runtime policies.

Do not connect generated MAP production, route/camera transitions, room crossing, item interactions, carry/drop/throw, bomb, rope, hazards, death, HUD, audio, animation, save data, PlayMode tests, build settings, or final build validation.

## Entry Gate

Verify:

```text
Current Task: CLI/MCP/TASKS/L01_03.md
L01_02 RESULT exists
L01_02 RESULT sha256: 906657a671a710336a64a508f9f213d8172acfd855269efe67797fe25cabb6f3
L01_02 RESULT contains STATUS: PASS
L01_02 RESULT contains None. Prefab and manual scene composition only.
L01_02 RESULT contains No generated run spawn wiring
Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab exists
Assets/_Game/Scenes/Live/CharacterLiveTest.unity exists
Assets/_Game/Live/Runtime/Player/CharacterLivePlayerRig.cs exists
CLI/MCP/INPUTS/LIVE_LOCK.md exists
CLI/MCP/INPUTS/LIVE_LOCK.md contains LOCK_STATE: FILLED_BY_L00_02
L02_01 and later tasks are locked
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
9. `CLI/MCP/REPORTS/L01_02_RESULT.md`
10. `CharacterDesign/MCP/REPORTS/CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES_RESULT.md`
11. `CharacterDesign/MCP/REPORTS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS_RESULT.md`
12. `CharacterDesign/MCP/REPORTS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT_RESULT.md`
13. `CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`
14. `Assets/_Game/Character/Runtime/Input/**`
15. `Assets/_Game/Character/Runtime/State/**`
16. `Assets/_Game/Character/Runtime/Movement/**`
17. `Assets/_Game/Character/Runtime/MapIntegration/**`
18. `Assets/_Game/Character/Runtime/Integration/**`
19. `Assets/_Game/Character/Runtime/RunState/**`
20. `Assets/_Game/Live/Runtime/**`
21. `Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab`
22. `Assets/_Game/Scenes/Live/CharacterLiveTest.unity`
23. `Assets/_Game/Map/Runtime/**`
24. `Packages/manifest.json`

Use search before opening broad trees. Prefer exact existing constructors/factories over guessed APIs. If exact Character API names differ from the reports, use the local source as authority and record the mapping.

## Allowed Writes

```text
Assets/_Game/Live/Runtime/**
Assets/_Game/Live/Prefabs/**
Assets/_Game/Scenes/Live/**
CLI/MCP/REPORTS/L01_03_RESULT.md
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

Create a live run bootstrap layer.

Required behavior:

```text
Create or adapt a live spawn request consumer that applies CharacterPlayerSpawnRequest to CharacterLivePlayerRig exactly once per run start.
Use CharacterSpawnIntegrationPolicy and CharacterGeneratedMapStartSnapshot when producing the initial spawn request from a fixed/manual start snapshot.
Use CharacterPlayerSpawnRequest.WorldCenter as the only spawn position source.
Initialize a live run session with actor id, current room id, and CharacterRunState or the nearest existing run-state contract.
Expose run started / spawn consumed state for later L02 and L03 tasks.
Wire CharacterLiveTest scene to the bootstrap and existing CharacterLivePlayer prefab.
Keep EditorBuildSettings unchanged.
```

Create a FixedUpdate movement driver:

```text
Consume input through CharacterLivePlayerRig.ConsumeFixedSnapshot(tick).
Evaluate ground state through existing Character movement collision/world-query contracts.
Apply ground movement, jump, variable jump release, air control, gravity, and landing using existing Character runtime policies.
Move the kinematic Rigidbody2D through a deterministic live adapter such as MovePosition or velocity integration.
Keep the Character runtime as the authority for movement math and state transitions.
Do not implement dash, wall jump, double jump, shooting, melee, or basic attack fallback paths.
```

Recommended files, unless repo context proves a better local pattern:

```text
Assets/_Game/Live/Runtime/Run/CharacterLiveRunSession.cs
Assets/_Game/Live/Runtime/Run/CharacterLiveManualStartSource.cs
Assets/_Game/Live/Runtime/Run/CharacterLiveSpawnConsumer.cs
Assets/_Game/Live/Runtime/Run/CharacterLiveRunBootstrap.cs
Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementDriver.cs
Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementSettings.cs
```

Allowed prefab/scene updates:

```text
Add movement driver and spawn consumer references to CharacterLivePlayer prefab if the design keeps player-local consumers there.
Add run bootstrap and fixed/manual start source to CharacterLiveTest scene.
Keep the scene as a manual smoke scene, not a build scene.
```

Manual start constraints:

```text
Manual start is a temporary L01_03 source only.
It must use existing WorldTileCoord/CharacterRoomId/CharacterGeneratedMapStartSnapshot contracts.
It must not generate MAP rooms, microchunks, routes, items, or Tilemaps.
It must be replaceable by the L02_02 MAP adapter without changing Character runtime.
```

## Required Verification

Run or report the nearest available equivalents:

```text
Unity compile: PASS required
Character EditMode baseline: 177/177 PASS required unless runner unavailable for an approved environment reason
Prefab audit: live player has rig, input source, kinematic body, collider, movement driver or documented external driver binding
Scene audit: CharacterLiveTest has one player prefab instance, one bootstrap, one manual start source, one camera, no build setting change
Spawn audit: bootstrap consumes exactly one CharacterPlayerSpawnRequest and places player at request.WorldCenter
Movement smoke audit: horizontal input and jump input reach the movement driver through CharacterInputSnapshot
Input binding audit: L01_01 inputactions unchanged, no E/F/Q
Forbidden feature audit: no basic attack, melee, shoot, dash, wall jump, double jump, or new ActionId
Scope audit: changed files only in allowed paths plus result
```

MAP EditMode 13,536 may remain anchored unless local policy requires rerun, because this task must not touch MAP runtime.

PlayMode tests are not required until L04_01. A short manual Play Mode smoke is allowed if the MCP environment can run it reliably, but do not create PlayMode test files in this task.

If Unity cannot run, write `STATUS: BLOCKED` unless the MCP rules explicitly allow an environment-only substitute.

## Required Report

Write:

```text
CLI/MCP/REPORTS/L01_03_RESULT.md
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

For this task, `REQUESTS_CONSUMED` must include:

```text
CharacterPlayerSpawnRequest consumed exactly once per run start.
No route, camera, room transition, bomb, rope, damage, death, run failure, HUD, or presentation requests consumed.
```

For this task, `ASSETS_WIRED` must include:

```text
CharacterLiveControls.inputactions -> CharacterLiveInputSource -> CharacterLivePlayerRig -> CharacterLiveMovementDriver
CharacterGeneratedMapStartSnapshot/manual source -> CharacterSpawnIntegrationPolicy -> CharacterPlayerSpawnRequest -> CharacterLiveSpawnConsumer -> CharacterLivePlayer prefab instance
CharacterLiveRunBootstrap -> CharacterLiveTest scene
No generated MAP adapter wiring
```

## Completion

PASS requires:

```text
Spawn request consumer implemented
Manual start source or equivalent fixed start snapshot implemented
Live run bootstrap implemented and wired to CharacterLiveTest
Movement driver consumes live input and delegates movement math to existing Character policies
Compile clean or environment-approved equivalent
Character baseline preserved
No MAP runtime, Character runtime, input asset, tests, build settings, HUD, route/camera, item/tool, save/audio/animation changes
```

If PASS:

```text
Finalize L01_03 as COMPLETE.
Current Task after finalize: NONE
Do not auto-open L02_01_ROUTE_CAMERA.
```
