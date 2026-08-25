# TASK: L02_01_ROUTE_CAMERA

Full name:

```text
LIVE02_01_CONSUME_ROUTE_AND_CAMERA_ROOM_TRANSITION_REQUESTS
```

## Objective

Wire the live run to room boundary detection, declared route validation, current-room updates, and camera-room movement.

This task consumes route/camera transition requests in the manual live scene only. It must not implement the generated MAP adapter yet. `L02_02_MAP_ADAPTER` owns replacing the manual room/route source with real generated MAP output.

Do not create or edit generated MAP production, Tilemaps, MAP runtime, pure Character runtime, input bindings, item systems, carry/drop/throw, bomb, rope, hazards, death, HUD, audio, animation, save data, PlayMode tests, build settings, or final build validation.

## Entry Gate

Verify:

```text
Current Task: CLI/MCP/TASKS/L02_01.md
L01_03 RESULT exists
L01_03 RESULT sha256: 6c652ca2728bb3a36a51048371494aac3d917b219627ef7c0ff010ab669da88b
L01_03 RESULT contains STATUS: PASS
L01_03 RESULT contains CharacterPlayerSpawnRequest consumed exactly once per run start.
L01_03 RESULT contains No generated MAP adapter wiring
Assets/_Game/Live/Runtime/Run/CharacterLiveRunSession.cs exists
Assets/_Game/Live/Runtime/Run/CharacterLiveRunBootstrap.cs exists
Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementDriver.cs exists
Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab exists
Assets/_Game/Scenes/Live/CharacterLiveTest.unity exists
CLI/MCP/INPUTS/LIVE_LOCK.md exists
CLI/MCP/INPUTS/LIVE_LOCK.md contains LOCK_STATE: FILLED_BY_L00_02
L02_02 and later tasks are locked
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
10. `CLI/MCP/REPORTS/L01_03_RESULT.md`
11. `CharacterDesign/MCP/REPORTS/CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY_RESULT.md`
12. `CharacterDesign/MCP/REPORTS/CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT_RESULT.md`
13. `CharacterDesign/MCP/REPORTS/CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES_RESULT.md`
14. `CharacterDesign/MCP/REPORTS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS_RESULT.md`
15. `CharacterDesign/MCP/REPORTS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT_RESULT.md`
16. `CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`
17. `Assets/_Game/Character/Runtime/MapIntegration/**`
18. `Assets/_Game/Character/Runtime/RoomTransition/**`
19. `Assets/_Game/Character/Runtime/Integration/**`
20. `Assets/_Game/Character/Runtime/RunState/**`
21. `Assets/_Game/Live/Runtime/**`
22. `Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab`
23. `Assets/_Game/Scenes/Live/CharacterLiveTest.unity`
24. `Assets/_Game/Map/Runtime/**`
25. `Packages/manifest.json`
26. `ProjectSettings/EditorBuildSettings.asset`

Use search before opening broad trees. Local source signatures are authority if they differ from reports.

## Allowed Writes

```text
Assets/_Game/Live/Runtime/**
Assets/_Game/Live/Prefabs/**
Assets/_Game/Scenes/Live/**
CLI/MCP/REPORTS/L02_01_RESULT.md
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

Create the live route/camera room layer.

Required behavior:

```text
Sample the live player position from CharacterLivePlayerRig or CharacterLiveMovementDriver.
Evaluate room boundary crossing with CharacterCameraRoomTransitionPolicy.
Use CharacterRoomBoundaryGate and ICharacterRoomReadinessSource through existing Character contracts; do not duplicate readiness logic.
Use declared route data through CharacterGeneratedRouteEdgeSnapshot and CharacterRouteIntegrationPolicy before consuming a route transition.
Consume exactly one transition for one stabilized boundary crossing after hysteresis.
Update CharacterLiveRunSession.CurrentRoomId only after an allowed transition request.
Move the scene camera to the target room center after the transition request is accepted.
Keep player input and velocity unchanged by the transition layer.
Do not teleport the player during ordinary camera-room transitions.
Block missing, unprepared, or undeclared target routes by not updating room/camera state and by recording diagnostics.
Support reverse crossing after the policy hysteresis allows it.
Expose last transition/diagnostic state for L02_03 audit and L04_01 PlayMode tests.
```

Required manual scene source:

```text
Extend CharacterLiveTest with at least two adjacent manual rooms and one declared route edge.
The manual room/route source is temporary for L02_01 only.
It must use existing CharacterRoomId, WorldTileCoord, WorldGenConstants, CharacterGeneratedRouteEdgeSnapshot, and readiness contracts.
It must not generate rooms, microchunks, items, Tilemaps, or MAP output.
It must be replaceable by L02_02 MAP adapter without changing pure Character runtime.
```

Required camera behavior:

```text
Use the existing scene Camera.
Use a deterministic room-center resolver based on MAP public coordinate/constant contracts.
Snap or deterministic-step the camera to the accepted target room center.
Do not require Cinemachine.
Do not alter velocity, input buffer, inventory, health, run state counts, save data, audio, or animation.
```

Recommended files, unless repo context proves a better local pattern:

```text
Assets/_Game/Live/Runtime/Rooms/CharacterLiveRoomReadinessSource.cs
Assets/_Game/Live/Runtime/Rooms/CharacterLiveManualRouteSource.cs
Assets/_Game/Live/Runtime/Rooms/CharacterLiveRoomCenterResolver.cs
Assets/_Game/Live/Runtime/Rooms/CharacterLiveRouteTransitionConsumer.cs
Assets/_Game/Live/Runtime/Rooms/CharacterLiveRoomTransitionDriver.cs
Assets/_Game/Live/Runtime/Camera/CharacterLiveCameraRoomDriver.cs
```

Allowed prefab/scene updates:

```text
Add room transition driver references to CharacterLivePlayer prefab only if player-local sampling is the local pattern.
Add manual room readiness/source, route source, route transition consumer, and camera driver to CharacterLiveTest scene.
Add a second manual floor/room marker if needed for visual smoke.
Keep EditorBuildSettings unchanged.
```

## Required Verification

Run or report the nearest available equivalents:

```text
Unity compile: PASS required
Character EditMode baseline: 177/177 PASS required unless runner unavailable for an approved environment reason
Prefab audit: live player still has rig/input/body/collider/movement driver and no forbidden components
Scene audit: CharacterLiveTest has one player prefab instance, one run bootstrap, one camera, manual room/route source, transition consumer, and camera driver
Route/camera smoke: crossing a prepared declared boundary updates CurrentRoomId once and moves camera to target room center
KEEP smoke: transition does not clear input or alter movement velocity
Blocked smoke: missing, unprepared, or undeclared route does not update CurrentRoomId or camera
Reverse smoke: crossing back after hysteresis updates current room and camera once
Input binding audit: L01_01 inputactions unchanged, no E/F/Q
Forbidden feature audit: no basic attack, melee, shoot, dash, wall jump, double jump, or new ActionId
Scope audit: changed files only in allowed paths plus result
```

MAP EditMode 13,536 may remain anchored unless local policy requires rerun, because this task must not touch MAP runtime.

PlayMode test files are not required until L04_01. A manual Play Mode smoke is expected if the MCP environment can run it reliably.

If Unity cannot run, write `STATUS: BLOCKED` unless the MCP rules explicitly allow an environment-only substitute.

## Required Report

Write:

```text
CLI/MCP/REPORTS/L02_01_RESULT.md
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
CharacterRoomTransitionRequest and CharacterGeneratedRouteTransitionRequest consumed by live route/camera layer.
No generated MAP adapter, carry, drop, throw, bomb, rope, damage, death, run failure, HUD, or presentation requests consumed.
```

For this task, `ASSETS_WIRED` must include:

```text
CharacterLiveMovementDriver/player position -> CharacterCameraRoomTransitionPolicy -> CharacterRoomTransitionRequest
Manual route source -> CharacterRouteIntegrationPolicy -> CharacterGeneratedRouteTransitionRequest -> CharacterLiveRouteTransitionConsumer
CharacterLiveRouteTransitionConsumer -> CharacterLiveRunSession.CurrentRoomId
Accepted target room -> CharacterLiveCameraRoomDriver -> CharacterLiveTest camera
No generated MAP adapter wiring
```

## Completion

PASS requires:

```text
Prepared declared boundary crossing produces one live route/camera transition
CurrentRoomId updates only after accepted transition
Camera moves to target room center only after accepted transition
Input and velocity KEEP are preserved
Missing/unprepared/undeclared route blocks transition
Reverse transition works after hysteresis
Compile clean or environment-approved equivalent
Character baseline preserved
No generated MAP adapter, MAP runtime, Character runtime, input asset, tests, build settings, HUD, item/tool, save/audio/animation changes
```

If PASS:

```text
Finalize L02_01 as COMPLETE.
Current Task after finalize: NONE
Do not auto-open L02_02_MAP_ADAPTER.
```
