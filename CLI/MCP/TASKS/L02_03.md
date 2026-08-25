# TASK: L02_03_ROOM_AUDIT

Full name:

```text
LIVE02_03_AUDIT_LIVE_MAP_ROOM_ROUTE_CAMERA_INTEGRATION
```

## Objective

Perform the LIVE02 exit audit for live map, room, route, camera, and generated
MAP adapter integration readiness.

This task is report-only. Do not implement features, wire scenes or prefabs,
edit assets, change tests, update package settings, or open LIVE03.

## Entry Gate

Verify:

```text
Current Task: CLI/MCP/TASKS/L02_03.md
L02_01 RESULT exists
L02_01 RESULT sha256: a0e4288ba390cbed70263681efd7ee235ee84a82baa3b434f2a0ad15309e4585
L02_01 RESULT contains STATUS: PASS
L02_01 RESULT contains CharacterRoomTransitionRequest and CharacterGeneratedRouteTransitionRequest consumed by live route/camera layer.
L02_01 RESULT contains No generated MAP adapter wiring
L02_02 RESULT exists
L02_02 RESULT sha256: 01bfe28aa2f4bf00245cecae1000ba4ab383cea4a8c297fe962f69ce33ba61d6
L02_02 RESULT contains STATUS: PASS
L02_02 RESULT contains None. Generated MAP adapter produces Character snapshots/readiness/routes/world query only.
L02_02 RESULT contains No scene or prefab wiring
L02_02 RESULT contains Current Task after finalize: NONE
CLI/MCP/INPUTS/LIVE_LOCK.md exists
CLI/MCP/INPUTS/LIVE_LOCK.md contains LOCK_STATE: FILLED_BY_L00_02
L03_01 and later tasks are locked
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
8. `CLI/MCP/REPORTS/L01_03_RESULT.md`
9. `CLI/MCP/REPORTS/L02_01_RESULT.md`
10. `CLI/MCP/REPORTS/L02_02_RESULT.md`
11. `CharacterDesign/MCP/REPORTS/CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES_RESULT.md`
12. `CharacterDesign/MCP/REPORTS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS_RESULT.md`
13. `CharacterDesign/MCP/REPORTS/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD_RESULT.md`
14. `CharacterDesign/MCP/REPORTS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT_RESULT.md`
15. `CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`
16. `Assets/_Game/Character/Runtime/Integration/**`
17. `Assets/_Game/Character/Runtime/GeneratedRunValidation/**`
18. `Assets/_Game/Character/Runtime/MapIntegration/**`
19. `Assets/_Game/Character/Runtime/RoomTransition/**`
20. `Assets/_Game/Character/Runtime/RunState/**`
21. `Assets/_Game/Live/Runtime/Rooms/**`
22. `Assets/_Game/Live/Runtime/Run/**`
23. `Assets/_Game/Live/Runtime/Adapters/Map/**`
24. `Assets/_Game/Live/Runtime/Movement/**`
25. `Assets/_Game/Live/Prefabs/**`
26. `Assets/_Game/Scenes/Live/**`
27. `Assets/_Game/Map/Runtime/**`
28. `Packages/manifest.json`
29. `ProjectSettings/ProjectSettings.asset`

Use search before opening broad trees. Local source signatures are authority if
they differ from prior reports.

## Allowed Writes

```text
CLI/MCP/REPORTS/L02_03_RESULT.md
```

Forbidden writes:

```text
Assets/**
Packages/**
ProjectSettings/**
MapDesign/**
CharacterDesign/**
CLI/MCP/STATUS.md
CLI/MCP/MASTER.md
CLI/MCP/TASKS/**
CLI/MCP/INPUTS/**
Builds/**
Temp/**
```

## Required Audit

Verify prior task evidence:

```text
L02_01_RESULT.md is PASS and matches the required sha256.
L02_02_RESULT.md is PASS and matches the required sha256.
Both reports state Current Task after finalize: NONE.
No future task was auto-opened.
```

Audit route and camera behavior from L02_01:

```text
CharacterRoomTransitionRequest and CharacterGeneratedRouteTransitionRequest are the only route/camera requests consumed by the live route/camera layer.
Room transition is gated by readiness, declared edge, boundary direction, and stabilization.
Missing, unprepared, undeclared, reverse, or unstable transitions are blocked.
Transition preserves KEEP input and velocity policy from Character contracts.
Camera target resolves from room center data without teleporting the player.
Manual route/readiness sources are isolated and replaceable.
```

Audit generated MAP adapter behavior from L02_02:

```text
Generated MAP public output projects to CharacterGeneratedRunSnapshot.
Placed microchunks project to room, microchunk, and cell state data.
Start cell projects to CharacterGeneratedMapStartSnapshot only when valid.
Declared routes, items, and markers project deterministically.
Projected readiness source implements the same surface used by L02_01.
Projected route source exposes DeclaredEdges and readiness in the same shape used by L02_01.
World query adapter distinguishes generated empty cells from ungenerated cells.
Ungenerated cells return false, not empty playable space.
Validation is delegated to existing Character generated-run validation policy.
Adapter produces snapshots/readiness/routes/world query only and consumes no requests.
```

Audit compatibility:

```text
Generated readiness source can replace CharacterLiveRoomReadinessSource.
Generated route source can replace CharacterLiveManualRouteSource.
CharacterLiveRouteTransitionConsumer can use projected route/readiness data without contract changes.
CharacterLiveCameraRoomDriver can continue to use room center resolution without MAP mutation.
Scene/prefab wiring is intentionally absent and belongs to a later package.
No manual source behavior is required once generated adapter is wired in a later task.
```

Audit dependency direction and scope:

```text
Live may depend on Character runtime and MAP runtime public contracts.
Character runtime must not depend on Live.
MAP runtime must not depend on Character or Live.
No MAP runtime facade or generator rewrite was added.
No Character runtime changes were made in LIVE02.
No Tilemap, scene, prefab, input asset, tests, build settings, save, audio, animation, HUD, item/tool, damage, death, or run-failure behavior was changed.
```

Audit baseline:

```text
Character EditMode baseline remains 177/177 PASS.
MAP EditMode 13,536-test anchor remains valid because MAP runtime was not touched.
Compile remains clean or the latest accepted compile evidence is cited.
If local policy requires reruns, run the nearest equivalent commands and record exact results.
```

## Required Report

Write:

```text
CLI/MCP/REPORTS/L02_03_RESULT.md
```

Include:

```text
TASK
STATUS
SUMMARY
READ
CHANGED
CREATED
TESTS
BUILD
PRIOR_RESULTS
ROUTE_CAMERA_AUDIT
GENERATED_MAP_ADAPTER_AUDIT
COMPATIBILITY_AUDIT
DEPENDENCY_DIRECTION
SCOPE_VALIDATION
FORBIDDEN_AUDIT
LIVE02_EXIT
NEXT
```

For this report-only task:

```text
CHANGED must be None or empty.
CREATED must contain only CLI/MCP/REPORTS/L02_03_RESULT.md.
REQUESTS_CONSUMED, if included, must be None.
ASSETS_WIRED, if included, must be None.
```

## Completion

PASS requires:

```text
L02_01 and L02_02 results are verified by status and sha256.
Route/camera consumers and generated MAP adapter are compatible.
Generated readiness, route, and world query sources are ready for later wiring.
No code, asset, scene, prefab, input, test, package, project setting, MAP, or Character write occurred.
Dependency direction remains Live -> Character/MAP public contracts only.
Forbidden feature audit is clean.
LIVE02 can close without auto-opening LIVE03.
```

If PASS, include exactly:

```text
LIVE02_EXIT_DECISION: APPROVED
L03_01 ENTRY: ELIGIBLE FOR SEPARATE PACKAGE
Current Task after finalize: NONE
Next Task auto-opened: NO
```
