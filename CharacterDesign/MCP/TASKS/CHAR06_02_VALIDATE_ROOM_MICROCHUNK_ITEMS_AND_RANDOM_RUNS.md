# TASK: CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS

```yaml
task_id: CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS
phase: CHAR06_GENERATED_MAP_AND_FINAL_VALIDATION
task_type: VALIDATION_IMPLEMENTATION
created: 2026-08-24
workflow: MCP_INBOX_PATCH_ONLY
write_scope: LIMITED_CHARACTER_GENERATED_RUN_VALIDATION_RUNTIME_AND_TESTS
```

## Objective

Validate that generated MAP room, microchunk, item, route, bomb, rope, and random-run data can be consumed by the locked Character runtime without opening new gameplay features.

This task owns:

```text
generated run snapshot projection for character validation
room and microchunk bounds validation
generated route validation through CHAR06_01 integration requests
generated item placement validation around spawn, entry, exit, and blocked cells
bomb and rope affordance validation against locked equipment support
deterministic random seed sweep diagnostics
no MAP generation rewrite, no MAP mutation, no scene/prefab/Tilemap mutation, no live physics wiring, no UI, no audio, no save
```

This task must not perform full Unity PlayMode or build validation. `CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD` owns full compile, EditMode, PlayMode, and build validation.

## Entry Gate

Before changing anything, verify:

```text
Current Task: TASKS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS.md
CHAR06_01 Result: PASS
CHAR06_01 Result SHA-256: c93702d78bea0da3260a02594157b5dd40e764ae786325ee4dd93e753eb694ca
CHAR06_01 contains: 170/170 PASS
CHAR06_01 contains: Current Task after finalize: NONE
CHAR06_01 contains: CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS
CHAR06_01 contains: LOCKED 유지
Source Registry marker: REGISTRY_STATE: FILLED_BY_CHAR00_01
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR06_03 and CHAR06_04: LOCKED
```

If any entry gate is false, write a BLOCKED report and do not modify project code.

## Mandatory Read Order

Read these files in order:

1. `CharacterDesign/MCP/00_MCP_ENTRYPOINT.md`
2. `CharacterDesign/MCP/01_CHARACTER_LOCKED_RULES.md`
3. `CharacterDesign/MCP/02_MCP_WORK_RULES.md`
4. `CharacterDesign/MCP/03_CHARACTER_DATA_RULES.md`
5. `CharacterDesign/MCP/04_UNITY_MCP_RULES.md`
6. `CharacterDesign/MCP/05_CHANGE_CONTROL_RULES.md`
7. `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md`
8. `CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md`
9. `CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`
10. `CharacterDesign/MCP/TASKS/CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES.md`
11. `CharacterDesign/MCP/REPORTS/CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES_RESULT.md`
12. `CharacterDesign/MCP/REPORTS/CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT_RESULT.md`
13. `CharacterDesign/MCP/REPORTS/CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT_RESULT.md`
14. `CharacterDesign/MCP/REPORTS/CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT_RESULT.md`
15. `CharacterDesign/01_FIXED_SPEC/01_CHARACTER_GAMEPLAY_RULES.md`
16. `CharacterDesign/01_FIXED_SPEC/03_CHARACTER_MOVEMENT_RULES.md`
17. `CharacterDesign/01_FIXED_SPEC/04_CHARACTER_INTERACTION_RULES.md`
18. `CharacterDesign/01_FIXED_SPEC/05_CHARACTER_COMBAT_RULES.md`
19. `CharacterDesign/01_FIXED_SPEC/06_CHARACTER_MAP_INTEGRATION_RULES.md`
20. `CharacterDesign/01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
21. `CharacterDesign/03_DATA_SCHEMA/CHARACTER_ACTION_SCHEMA.md`
22. `CharacterDesign/03_DATA_SCHEMA/CHARACTER_INVENTORY_SCHEMA.md`
23. Current character runtime under `Assets/_Game/Character/Runtime/`
24. Current character EditMode tests under `Assets/_Game/Tests/EditMode/Character/`
25. MAP public generated map, room, route, item, microchunk, coordinate, and world-generation contracts from source registry only
26. MAP authoring fixture data from source registry only, especially generated room and microchunk authoring data
27. MAP validation test precedents under `Assets/_Game/Tests/EditMode/Map/` for read-only reference only

Do not read or start `CHAR06_03` or `CHAR06_04` task bodies.

## Allowed Writes

Allowed runtime writes:

```text
Assets/_Game/Character/Runtime/Integration/**
Assets/_Game/Character/Runtime/MapIntegration/**
Assets/_Game/Character/Runtime/GeneratedRunValidation/**
```

Allowed test writes:

```text
Assets/_Game/Tests/EditMode/Character/Integration/**
Assets/_Game/Tests/EditMode/Character/MapIntegration/**
Assets/_Game/Tests/EditMode/Character/GeneratedRunValidation/**
```

Conditional tiny adapter writes:

```text
Assets/_Game/Character/Runtime/RunState/**
Assets/_Game/Character/Runtime/Equipment/**
Assets/_Game/Tests/EditMode/Character/RunState/**
Assets/_Game/Tests/EditMode/Character/Equipment/**
```

Use conditional adapter writes only if a small read-only accessor is required to expose existing bomb, rope, inventory, or run-state values to validation policies. Do not rewrite movement, equipment, survival, presentation, combat, room transition, or MAP behavior.

Required report:

```text
CharacterDesign/MCP/REPORTS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS_RESULT.md
```

Forbidden:

- Runtime or test changes outside allowed GeneratedRunValidation, Integration, MapIntegration, and conditional adapter paths.
- asmdef changes unless compile proves the new files are not included by existing `Game.Character.Runtime` or `Game.Character.Tests.EditMode`; if asmdef change is unavoidable, BLOCK and report instead.
- Scene, prefab, physics layer asset, inputactions, Packages, ProjectSettings, MapDesign, MAP runtime, Tilemap, camera, animation, audio, UI, save data, or legacy code changes.
- Changing the MAP generator, MAP authoring CSV data, MAP route graph, MAP item generator, MAP validation tests, or MAP build outputs.
- Creating random rooms or microchunks inside Character runtime as a substitute for MAP output.
- Actual player GameObject spawning, transform movement, scene loading, camera transition, UI/HUD binding, audio/animation playback, MAP mutation, Tilemap writes, or route graph generation.
- Full PlayMode or build validation.
- Health, hazard, bomb, rope, combat, movement, room transition, run state, HUD, or presentation behavior changes beyond tiny read-only adapters.
- Adding a basic attack, melee, shoot, dash, wall jump, or double jump.
- Adding ActionId values beyond the existing locked set.
- Animator, Unity physics callback, Unity UI, audio, scene, save, or Tilemap authority.
- Opening or installing any future task.
- Editing `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md` or `CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md` during task execution.

## Required Implementation

### 1. Generated Run Snapshot Source

Implement character-side validation values that can consume generated MAP output without owning MAP generation.

Required behavior:

```text
generated run snapshot records run id, seed, rooms, routes, microchunks, item placements, start cell, and exit or goal markers
snapshot source uses MAP public contracts discovered from the source registry
if direct MAP runtime output is not public, define a narrow character-side snapshot interface and map only public MAP domain values into it
snapshot creation never edits MAP data and never writes Tilemap cells
invalid or incomplete source data creates diagnostics rather than exceptions where recoverable
```

### 2. Room and Microchunk Validation

Implement validation policies for room and microchunk structure.

Required behavior:

```text
every room id is unique
every room bounds rectangle is inside WorldGenConstants world bounds
every microchunk is aligned to MAP microchunk dimensions
every microchunk cell range is inside its owning room bounds
duplicate microchunk occupancy inside a room is rejected
route source and target rooms must exist
route source exit cell and target entry cell must be inside their declared rooms
route validation delegates transition request creation to CHAR06_01 route integration policy
```

### 3. Item Placement and Tool Affordance Validation

Implement generated item and tool affordance validation.

Required behavior:

```text
every generated item placement cell is inside the declared room and world bounds
item placement must not occupy player spawn cell, route entry cell, route exit cell, or explicitly blocked validation cells
item placement diagnostics identify item id, room id, cell, and reason
bomb-support route requirements are accepted only when generated run equipment or inventory state can supply bomb support
rope-support route requirements are accepted only when generated run equipment or inventory state can supply rope support
tool affordance validation does not spend inventory and does not instantiate items
unsupported movement or combat requirements are rejected through CHAR06_01 capability policy
```

### 4. Deterministic Random Seed Sweep

Implement a deterministic validation batch for generated runs.

Required behavior:

```text
validate a fixed seed list of at least 8 seeds
same seed and same generated snapshot input produce the same validation digest
seed sweep records pass, fail, and diagnostic counts per seed
failed seed reports must include seed, room id, route id or item id when available, and diagnostic reason
seed sweep must not hide failures, ignore tests, or mutate project assets
```

### 5. Authority and Scope Guard

Keep validation authority pure.

Required behavior:

```text
no Animator event authority
no Unity physics callback authority
no Unity UI authority
no audio authority
no SceneManager authority
no save or PlayerPrefs authority
no direct MAP or Tilemap mutation
no GameObject or prefab authority
no basic attack
no melee
no shoot
no dash
no wall jump
no double jump
ActionId locked set remains unchanged
```

## Required Test Coverage

Add deterministic EditMode tests covering at least these behaviors:

```text
GeneratedRoom_MicrochunksStayWithinRoomAndWorldBounds
GeneratedRoom_RoutesReferenceExistingRoomsAndCreateCharacterRequests
GeneratedItems_DoNotOccupySpawnEntryExitOrBlockedCells
GeneratedRun_BombAndRopeAffordancesMatchLockedCapabilities
RandomRun_SeedSweepIsDeterministicAndReportsReproducibleDiagnostics
GeneratedRunValidation_DoesNotMutateMapTilemapScenePrefabPlayerTransformRunStateInventoryOrAssets
GeneratedRunValidation_DoesNotUseAnimatorPhysicsUiAudioSceneSaveOrForbiddenActions
```

Names may vary if they fit existing conventions, but the report must map actual test names to these seven required behaviors.

Run:

```text
Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Expected minimum tests: 175
Expected result: PASS
```

The expected minimum is previous 170 plus at least 5 CHAR06_02 tests. More tests are allowed if they remain inside the task scope.

PlayMode and build validation are not required for this task.

## Required Report

Write:

```text
CharacterDesign/MCP/REPORTS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS_RESULT.md
```

The report must include:

- `TASK`
- independent line `STATUS: PASS`, `STATUS: FAIL`, or `STATUS: BLOCKED`
- `SUMMARY`
- `READ`
- `CHANGED`
- `CREATED`
- `TEST`
- `UNITY`
- `GENERATED_RUN_SNAPSHOT_SOURCE`
- `ROOM_MICROCHUNK_VALIDATION`
- `ITEM_AND_TOOL_AFFORDANCE_VALIDATION`
- `RANDOM_SEED_SWEEP`
- `AUTHORITY_AND_FORBIDDEN_FEATURE_GUARD`
- `DEPENDENCY_DIRECTION`
- `SCOPE_VALIDATION`
- `DEPENDENCY_LEDGER`
- `OUT_OF_SCOPE_FINDINGS`
- `DONE CONDITIONS`
- `NEXT`

Required report facts:

```text
Entry gate verification result
CHAR06_01 report hash used
source registry hash used
all files changed and created
actual generated snapshot source or adapter source used
seed list and per-seed result summary
new test names mapped to required behaviors
total Character EditMode test count and pass/fail/skip count
Unity compile error count
confirmation that MAP runtime, MAP authoring data, Tilemap, scene, prefab, ProjectSettings, Packages, inputactions, UI, audio, save, and legacy code were not changed
confirmation that CHAR06_03 remains locked
```

PASS requires all required tests passing, compile errors 0, no scope violations, and no hidden failures.

## Completion and Finalization Rule

If PASS:

```text
Finalize CHAR06_02 as COMPLETE.
Set Current Task after finalize: NONE.
Do not auto-open CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD.
Keep CHAR06_03 and CHAR06_04 LOCKED until a new MCP_INBOX package opens CHAR06_03.
```

If FAIL or BLOCKED:

```text
Keep CHAR06_02 as CURRENT.
Do not open CHAR06_03.
Report exact failed seed, missing contract, compile error, or scope violation.
```

