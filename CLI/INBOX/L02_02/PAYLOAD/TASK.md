# TASK: L02_02_MAP_ADAPTER

Full name:

```text
LIVE02_02_PROJECT_GENERATED_MAP_OUTPUT_TO_CHARACTER_RUN_SNAPSHOT
```

## Objective

Implement the live-side adapter that projects generated MAP output into the completed Character generated-run contracts.

This task must keep MAP runtime read-only. Do not add a MAP facade, rewrite the MAP generator, edit MAP data, edit Tilemaps, or copy generation logic into Character/Live. The adapter belongs only under `Assets/_Game/Live/Runtime/Adapters/**`.

Do not wire the adapter into the live scene yet unless the task's allowed paths already contain that scene, which they do not. `L02_03_ROOM_AUDIT` owns the live MAP/room audit after this adapter exists.

## Entry Gate

Verify:

```text
Current Task: CLI/MCP/TASKS/L02_02.md
L02_01 RESULT exists
L02_01 RESULT sha256: a0e4288ba390cbed70263681efd7ee235ee84a82baa3b434f2a0ad15309e4585
L02_01 RESULT contains STATUS: PASS
L02_01 RESULT contains CharacterRoomTransitionRequest and CharacterGeneratedRouteTransitionRequest consumed by live route/camera layer.
L02_01 RESULT contains No generated MAP adapter wiring
Assets/_Game/Live/Runtime/Rooms/CharacterLiveManualRouteSource.cs exists
Assets/_Game/Live/Runtime/Rooms/CharacterLiveRoomReadinessSource.cs exists
Assets/_Game/Live/Runtime/Rooms/CharacterLiveRouteTransitionConsumer.cs exists
CLI/MCP/INPUTS/LIVE_LOCK.md exists
CLI/MCP/INPUTS/LIVE_LOCK.md contains LOCK_STATE: FILLED_BY_L00_02
L02_03 and later tasks are locked
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
10. `CharacterDesign/MCP/REPORTS/CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES_RESULT.md`
11. `CharacterDesign/MCP/REPORTS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS_RESULT.md`
12. `CharacterDesign/MCP/REPORTS/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD_RESULT.md`
13. `CharacterDesign/MCP/REPORTS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT_RESULT.md`
14. `CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`
15. `Assets/_Game/Map/Runtime/**`
16. `Assets/_Game/Character/Runtime/Integration/**`
17. `Assets/_Game/Character/Runtime/GeneratedRunValidation/**`
18. `Assets/_Game/Character/Runtime/MapIntegration/**`
19. `Assets/_Game/Character/Runtime/RunState/**`
20. `Assets/_Game/Live/Runtime/Rooms/**`
21. `Assets/_Game/Live/Runtime/Run/**`
22. `Assets/_Game/Live/Runtime/Adapters/**` if it already exists
23. `Packages/manifest.json`

Use search before opening broad trees. Local source signatures are authority if they differ from reports.

## Allowed Writes

```text
Assets/_Game/Live/Runtime/Adapters/**
CLI/MCP/REPORTS/L02_02_RESULT.md
```

Forbidden writes:

```text
Assets/_Game/Character/Runtime/**
Assets/_Game/Map/Runtime/**
Assets/_Game/Live/Input/**
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

Create a live generated MAP adapter.

Required projection behavior:

```text
Accept runtime-accessible generated MAP data only through public MAP runtime contracts.
Project MAP start cell/room into CharacterGeneratedMapStartSnapshot.
Project MAP rooms into CharacterGeneratedRoomSnapshot.
Project MAP microchunks into CharacterGeneratedMicrochunkSnapshot.
Project MAP declared exits/routes into CharacterGeneratedRouteEdgeSnapshot.
Project MAP item placements and exit markers into CharacterGeneratedRunSnapshot when public source data exists.
Produce CharacterGeneratedRunSnapshot deterministically for the same MAP output.
Preserve deterministic ordering for rooms, microchunks, routes, item placements, exit markers, and diagnostics.
Validate the projected snapshot with existing Character generated-run validation policies when available.
Expose an ICharacterRoomReadinessSource-compatible generated readiness source for L02_01 route/camera consumers.
Expose a generated route source compatible with CharacterLiveRouteTransitionConsumer.
Expose diagnostics for missing start, out-of-bounds rooms, route cell mismatch, missing route target, missing public MAP source fields, and unsupported route requirements.
```

Required world query behavior:

```text
If MAP public tile/cell output is available, expose an ICharacterMapWorldQuery-compatible live adapter.
Unknown or ungenerated cells must not be treated as ordinary empty playable space.
Do not mutate Tilemap or MAP data.
Do not duplicate MAP coordinate math; use WorldTileCoord, WorldCoordinateUtility, WorldGenConstants, and existing Character map bridge contracts.
```

Allowed absence handling:

```text
If a complete runtime generated-map facade exists, adapt it directly.
If only lower-level public MAP domain values exist, create a narrow live-side input interface under Adapters and implement projection from those public values.
If no runtime-accessible public generated MAP data exists at all, write STATUS: BLOCKED with the exact missing public source and do not create substitute generation logic.
Do not reference MAP test assemblies or test fixtures from runtime code.
```

Recommended files, unless repo context proves a better local pattern:

```text
Assets/_Game/Live/Runtime/Adapters/Map/CharacterLiveGeneratedMapAdapter.cs
Assets/_Game/Live/Runtime/Adapters/Map/CharacterLiveGeneratedMapAdapterInput.cs
Assets/_Game/Live/Runtime/Adapters/Map/CharacterLiveGeneratedMapDiagnostics.cs
Assets/_Game/Live/Runtime/Adapters/Map/CharacterLiveGeneratedReadinessSource.cs
Assets/_Game/Live/Runtime/Adapters/Map/CharacterLiveGeneratedRouteSource.cs
Assets/_Game/Live/Runtime/Adapters/Map/CharacterLiveMapWorldQueryAdapter.cs
```

## Required Verification

Run or report the nearest available equivalents:

```text
Unity compile: PASS required
Character EditMode baseline: 177/177 PASS required unless runner unavailable for an approved environment reason
Adapter projection smoke: public MAP-shaped sample projects to CharacterGeneratedRunSnapshot with one valid start, rooms, microchunks, routes, and deterministic digest
Validation smoke: existing Character generated-run validation accepts the projected valid sample and reports diagnostics for invalid sample
Readiness smoke: generated readiness source reports prepared/missing rooms as expected
Route source smoke: projected routes can feed CharacterRouteIntegrationPolicy without manual route source
World query smoke: if public tile/cell data exists, solid/empty/unknown cells resolve through ICharacterMapWorldQuery-compatible adapter
Forbidden dependency audit: runtime code references MAP runtime public contracts only, never MAP tests, editor-only APIs, Tilemap mutation, scene lookup, or generator internals outside public contracts
Scope audit: changed files only in allowed paths plus result
```

MAP EditMode 13,536 may remain anchored unless local policy requires rerun, because MAP runtime must not be touched.

Do not create PlayMode test files in this task. A manual editor smoke through temporary in-memory samples is allowed.

If Unity cannot run, write `STATUS: BLOCKED` unless the MCP rules explicitly allow an environment-only substitute.

## Required Report

Write:

```text
CLI/MCP/REPORTS/L02_02_RESULT.md
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
None. Generated MAP adapter produces Character snapshots/readiness/routes/world query only.
No spawn, route/camera, carry, drop, throw, bomb, rope, damage, death, run failure, HUD, or presentation requests consumed.
```

For this task, `ASSETS_WIRED` must include:

```text
Generated MAP public output -> CharacterGeneratedRunSnapshot adapter
Generated MAP projected rooms -> generated readiness source
Generated MAP projected routes -> generated route source
Generated MAP projected tiles/cells -> live map world query if source data exists
No scene or prefab wiring
```

## Completion

PASS requires:

```text
Generated MAP adapter implemented under Assets/_Game/Live/Runtime/Adapters/**
Projection to CharacterGeneratedRunSnapshot works for public MAP-shaped runtime data
Readiness source and route source are compatible with L02_01 route/camera consumers
World query adapter exists or a precise non-blocking absence reason is reported if tile/cell source data is unavailable
Compile clean or environment-approved equivalent
Character baseline preserved
No MAP runtime, Character runtime, scene, prefab, input asset, tests, build settings, HUD, item/tool, save/audio/animation changes
```

If PASS:

```text
Finalize L02_02 as COMPLETE.
Current Task after finalize: NONE
Do not auto-open L02_03_ROOM_AUDIT.
```
