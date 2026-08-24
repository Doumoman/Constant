# TASK: CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES

```yaml
task_id: CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES
phase: CHAR06_GENERATED_MAP_AND_FINAL_VALIDATION
task_type: IMPLEMENTATION
created: 2026-08-24
workflow: MCP_INBOX_PATCH_ONLY
write_scope: LIMITED_GENERATED_MAP_CHARACTER_INTEGRATION_RUNTIME_AND_TESTS
```

## Objective

Implement the pure integration contract between Character runtime and generated MAP start/route data.

This task owns:

```text
generated map start snapshot -> player spawn request
generated map room/route snapshot -> route transition request
route readiness gate using CHAR03 room transition policy
route capability check using locked movement/equipment capabilities
route requirement output for bomb/rope-only route affordances
deterministic integration request batch
no scene, prefab, Tilemap, MAP mutation, live physics wiring, UI, audio, save, or GameObject mutation
```

This task must not validate random rooms/microchunks/items across many seeds. `CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS` owns generated room, microchunk, item, and random-run validation.

## Entry Gate

Before changing anything, verify:

```text
Current Task: TASKS/CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES.md
CHAR05_05 Result: PASS
CHAR05_05 Result SHA-256: cb7f4d136e6ff09183065754f4a22a1da4deab1311c80c7e205489e7cb0b17a6
CHAR05_05 contains: CHAR05_EXIT_DECISION: APPROVED
CHAR05_05 contains: Current Task after finalize: NONE
CHAR05_05 contains: CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES
CHAR05_05 contains: LOCKED 유지
Source Registry marker: REGISTRY_STATE: FILLED_BY_CHAR00_01
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR06_02 and later tasks: LOCKED
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
10. `CharacterDesign/MCP/TASKS/CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT.md`
11. `CharacterDesign/MCP/REPORTS/CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT_RESULT.md`
12. `CharacterDesign/MCP/REPORTS/CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT_RESULT.md`
13. `CharacterDesign/MCP/REPORTS/CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT_RESULT.md`
14. `CharacterDesign/MCP/REPORTS/CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT_RESULT.md`
15. `CharacterDesign/01_FIXED_SPEC/01_CHARACTER_GAMEPLAY_RULES.md`
16. `CharacterDesign/01_FIXED_SPEC/02_CHARACTER_INPUT_RULES.md`
17. `CharacterDesign/01_FIXED_SPEC/05_CHARACTER_COMBAT_RULES.md`
18. `CharacterDesign/01_FIXED_SPEC/06_CHARACTER_MAP_INTEGRATION_RULES.md`
19. `CharacterDesign/01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
20. `CharacterDesign/03_DATA_SCHEMA/CHARACTER_ACTION_SCHEMA.md`
21. `CharacterDesign/03_DATA_SCHEMA/CHARACTER_INVENTORY_SCHEMA.md`
22. Current character runtime under `Assets/_Game/Character/Runtime/`
23. Current character EditMode tests under `Assets/_Game/Tests/EditMode/Character/`
24. MAP public generated map, room, coordinate, world query, route, and mutation contracts from source registry only
25. Legacy generated-map/player-spawn integration examples for read-only reference only:
    - `Assets/_Legacy/StarNight/Scripts/Runtime/Player/**`
    - `Assets/_Legacy/StarNight/Scripts/Runtime/Objects/**`
    - `Assets/_Legacy/_Game/Core/State/**`
    - `Assets/_Legacy/_Game/Player/Tests/**`

Do not read or start any `CHAR06_02`, `CHAR06_03`, or `CHAR06_04` task body.

## Allowed Writes

Allowed runtime writes:

```text
Assets/_Game/Character/Runtime/Integration/**
Assets/_Game/Character/Runtime/MapIntegration/**
```

Allowed test writes:

```text
Assets/_Game/Tests/EditMode/Character/Integration/**
Assets/_Game/Tests/EditMode/Character/MapIntegration/**
```

Conditional bridge writes:

```text
Assets/_Game/Character/Runtime/Movement/**
Assets/_Game/Character/Runtime/RunState/**
Assets/_Game/Character/Runtime/Presentation/**
Assets/_Game/Tests/EditMode/Character/Movement/**
Assets/_Game/Tests/EditMode/Character/RunState/**
Assets/_Game/Tests/EditMode/Character/Presentation/**
```

Use conditional bridge writes only if a tiny adapter is required to expose existing movement/run-state/presentation request values to generated-map integration. Do not rewrite movement, room transition, equipment, survival, presentation, combat, or MAP behavior.

Required report:

```text
CharacterDesign/MCP/REPORTS/CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES_RESULT.md
```

Forbidden:

- Runtime or test changes outside allowed Integration/MapIntegration paths and conditional bridge paths.
- asmdef changes unless compile proves the new files are not included by existing `Game.Character.Runtime` or `Game.Character.Tests.EditMode`; if asmdef change is unavoidable, BLOCK and report instead.
- Scene, prefab, physics layer asset, inputactions, Packages, ProjectSettings, MapDesign, MAP runtime, Tilemap, camera, animation, audio, UI, save data, or legacy code changes.
- Actual player GameObject spawning, transform movement, scene loading, camera transition, UI/HUD binding, audio/animation playback, MAP mutation, Tilemap writes, or route graph generation.
- Random map seed sweep, microchunk validation, item placement validation, or generated room batch validation.
- Health, hazard, bomb, rope, combat, movement, room transition, run state, HUD, or presentation behavior changes beyond tiny request adapters.
- Adding a basic attack, melee, shoot, dash, wall jump, or double jump.
- Adding ActionId values beyond the existing locked set.
- Animator, Unity physics callback, Unity UI, audio, scene, save, or Tilemap authority.
- Opening or installing any future task.
- Editing `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md` or `CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md` during task execution.

## Required Implementation

### 1. Generated Map Start Snapshot and Player Spawn Request

Implement pure generated-map start integration values.

Required behavior:

```text
generated map start snapshot records map/run id, start room id, start cell, and room bounds
valid start cell inside start room and world bounds -> player spawn request
invalid, missing, or out-of-bounds start -> no spawn request and diagnostic result
spawn request records actor id, start cell, world center, and start room id
spawn request does not instantiate, move, enable, disable, or mutate any GameObject
```

Use MAP public coordinate and room/bounds contracts only.

### 2. Generated Route Transition Request

Implement generated-route transition request values.

Required behavior:

```text
declared route edge from current room to target room -> route transition request
undeclared route edge -> no transition request and diagnostic result
route transition request records source room, target room, boundary side, target entry cell, and route id
request respects CHAR03 input KEEP, velocity KEEP, readiness gate, and hysteresis policy by data contract
request does not move camera, load scene, move player transform, or mutate MAP
```

### 3. Route Capability Check

Implement route capability evaluation using existing locked character capabilities.

Required behavior:

```text
basic route requiring only 2-cell jump/gap grammar is accepted by locked movement profile
route requiring dash, wall jump, double jump, shoot, or basic attack is rejected
route requiring bomb is accepted only when bomb count/request support is available
route requiring rope is accepted only when rope count/request support is available
route requirement output is diagnostic data only and does not spend inventory or mutate state
```

### 4. Integration Request Batch

Implement deterministic request batch output.

Required behavior:

```text
same generated map snapshot and player state -> same spawn/route/capability request batch
batch order is deterministic
duplicate equivalent requests in one batch are emitted once
invalid map/route data produces diagnostics rather than exceptions where recoverable
```

### 5. Authority and Forbidden Feature Guard

Keep integration authority pure.

Required behavior:

```text
no Animator event authority
no Unity physics callback authority
no Unity UI authority
no audio authority
no SceneManager authority
no save/PlayerPrefs authority
no direct MAP or Tilemap mutation
no GameObject/prefab authority
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
GeneratedMapStart_ValidStartCreatesPlayerSpawnRequest
GeneratedMapStart_InvalidOrOutOfBoundsStartCreatesDiagnosticOnly
GeneratedMapStart_SpawnRequestUsesMapCoordinateBridgeCenter
GeneratedRoute_DeclaredRouteCreatesTransitionRequest
GeneratedRoute_UndeclaredRouteIsRejected
GeneratedRoute_RespectsRoomTransitionReadinessContract
RouteCapability_BasicMovementRouteIsAccepted
RouteCapability_ForbiddenMovementOrAttackRequirementsAreRejected
RouteCapability_BombAndRopeRequirementsRequireAvailableSupport
IntegrationBatch_IsDeterministicOrderedAndDeduplicated
Integration_DoesNotMutateMapTilemapScenePrefabPlayerTransformOrRunState
IntegrationRuntime_DoesNotUseAnimatorPhysicsUiAudioSceneSaveOrForbiddenActions
```

Names may vary if they fit existing conventions, but the report must map actual test names to these twelve required behaviors.

Run:

```text
Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Expected minimum tests: 170
Expected result: PASS
```

The expected minimum is previous 158 plus at least 12 CHAR06_01 tests.

PlayMode is not required for this task.

## Required Report

Write:

```text
CharacterDesign/MCP/REPORTS/CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES_RESULT.md
```

The report must include:

```text
TASK
STATUS
SUMMARY
READ
CHANGED
CREATED
TEST
UNITY
GENERATED_MAP_START_AND_SPAWN
GENERATED_ROUTE_TRANSITION
ROUTE_CAPABILITY_CHECK
INTEGRATION_REQUEST_BATCH
AUTHORITY_AND_FORBIDDEN_FEATURE_GUARD
DEPENDENCY_DIRECTION
SCOPE_VALIDATION
DEPENDENCY_LEDGER
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
```

## Done Conditions

All done conditions must be checked in the report:

- [ ] CHAR05 EXIT approved and CHAR05_05 PASS/hash verified.
- [ ] Source registry marker/hash verified.
- [ ] Valid generated map start creates player spawn request.
- [ ] Invalid or out-of-bounds start creates diagnostic only.
- [ ] Spawn request uses public map coordinate bridge.
- [ ] Declared route creates transition request.
- [ ] Undeclared route is rejected.
- [ ] Route transition respects CHAR03 readiness/input/velocity contract by data.
- [ ] Basic movement route is accepted.
- [ ] Forbidden movement/attack route requirements are rejected.
- [ ] Bomb and rope route requirements require available support.
- [ ] Integration batch is deterministic, ordered, and deduplicated.
- [ ] Integration output does not mutate MAP, Tilemap, scene, prefab, player transform, run state, UI, audio, or save data.
- [ ] Animator events and physics callbacks are not authority.
- [ ] Forbidden basic attack/movement features remain absent.
- [ ] ActionId locked set remains unchanged.
- [ ] Character EditMode tests pass with at least 170 tests.
- [ ] Unity compile errors 0.
- [ ] Scope validation completed.
- [ ] CHAR06_02 remains locked.

## Completion Rule

If STATUS is PASS:

- Finalize CHAR06_01 to COMPLETE.
- Set Current Task after finalize to NONE.
- Keep `CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS` locked.
- Do not auto-open CHAR06_02.

If STATUS is FAIL or BLOCKED:

- Keep CHAR06_01 CURRENT.
- Do not open CHAR06_02.

The NEXT section must include:

```text
Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
```

only when PASS/finalized. If not PASS, state why the task remains CURRENT.
