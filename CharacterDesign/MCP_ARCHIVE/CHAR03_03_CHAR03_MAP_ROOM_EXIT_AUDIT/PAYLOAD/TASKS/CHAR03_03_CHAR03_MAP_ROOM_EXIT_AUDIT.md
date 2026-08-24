# TASK: CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT

```yaml
task_id: CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT
phase: CHAR03_MAP_ROOM_INTEGRATION
task_type: AUDIT
created: 2026-08-23
workflow: MCP_INBOX_PATCH_ONLY
write_scope: REPORT_ONLY
```

## Objective

Audit CHAR03 MAP/room integration and decide whether CHAR03 can exit.

This task does not implement fixes. It verifies that CHAR03_01 and CHAR03_02 together satisfy the locked character/MAP integration rules:

```text
Character consumes MAP public coordinate/query contracts only.
Prepared room crossing can request a camera-room transition.
Unprepared or missing destination room is blocked.
Input KEEP and velocity KEEP are preserved.
Hysteresis prevents boundary ping-pong.
High-speed and airborne boundary entry are handled by the same policy.
```

If any of these are not proven, reject CHAR03 exit and keep CHAR04 locked.

## Entry Gate

Before doing any audit work, verify:

```text
Current Task: TASKS/CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT.md
CHAR03_01 Result: PASS
CHAR03_01 Result SHA-256: 3a3009d76b6b89e5bae44b6d743f866b6209728c3509e22b3ad7332063b9317b
CHAR03_02 Result: PASS
CHAR03_02 Result SHA-256: a99a1ed377aed266632ee1da2245610cbcc97015a67af23bc31ac3fc81092082
Source Registry marker: REGISTRY_STATE: FILLED_BY_CHAR00_01
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR04_01 and later tasks: LOCKED
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
10. `CharacterDesign/MCP/TASKS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE.md`
11. `CharacterDesign/MCP/REPORTS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE_RESULT.md`
12. `CharacterDesign/MCP/TASKS/CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY.md`
13. `CharacterDesign/MCP/REPORTS/CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY_RESULT.md`
14. `CharacterDesign/01_FIXED_SPEC/06_CHARACTER_MAP_INTEGRATION_RULES.md`
15. `CharacterDesign/01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
16. `CharacterDesign/04_TEST_FIXTURES/ROOM_TRANSITION_COURSE_SPEC.md`
17. Current character runtime under `Assets/_Game/Character/Runtime/`
18. Current `Assets/_Game/Character/Runtime/MapIntegration/`
19. Current `Assets/_Game/Character/Runtime/RoomTransition/`
20. Current character EditMode tests under `Assets/_Game/Tests/EditMode/Character/`
21. MAP public coordinate/domain runtime under `Assets/_Game/Map/Runtime/WorldGeneration/Domain/`

Do not read or start any `CHAR04`, `CHAR05`, or `CHAR06` task body.

## Allowed Writes

Allowed:

```text
CharacterDesign/MCP/REPORTS/CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT_RESULT.md
```

Forbidden:

- Runtime source changes.
- Test source changes.
- asmdef changes.
- Scene, prefab, inputactions, Packages, ProjectSettings, MapDesign, MAP runtime, Tilemap, camera component, Cinemachine, animation, presentation, or legacy changes.
- Starting `CHAR04_01` or installing any `CHAR04_*` task file.
- Implementing fixes for findings discovered during the audit.
- Editing `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md` or `CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md` during task execution.

## Audit Gates

### 1. Prior Evidence and State

Verify:

- CHAR03_01 is COMPLETE and its report is PASS.
- CHAR03_02 is COMPLETE and its report is PASS.
- Current Task is exactly this task.
- Source registry marker/hash match.
- CHAR04_01 and later tasks remain locked.
- CHAR03_01/02 task file hashes match their opened payloads.

### 2. MAP Coordinate and Query Contract

Verify:

- Character runtime references `Game.Map.Runtime` only as the approved MAP dependency.
- Character runtime uses MAP public coordinate/domain APIs instead of duplicating MAP coordinate conversion.
- Out-of-bounds map coordinates are rejected without clamping.
- Character-facing read-only world query covers solid, one-way, hazard, liquid, breakable, and empty/passable.
- Character runtime does not depend on Tilemap, MAP authoring/editor/test assemblies, generator internals, or legacy code.
- MAP runtime does not reference Character runtime.

### 3. Room Boundary Readiness Gate

Verify:

- Prepared destination room allows crossing.
- Unprepared destination room blocks crossing.
- Missing destination room blocks crossing.
- Same-room interior movement is unaffected.
- Gate decision does not mutate input or velocity.
- Live generated-map query/readiness source is explicitly deferred and does not invalidate the pure contract.

### 4. Camera Room Transition Policy

Verify:

- Prepared boundary crossing requests a target camera room.
- Transition request contains source and target room.
- Transition request does not move the camera directly.
- Transition request does not mutate player position.
- Unprepared/missing destinations are blocked through the existing readiness gate, not duplicated logic.
- Actual camera component, Cinemachine, Scene, Prefab, animation, and presentation wiring remain out of scope.

### 5. Input and Velocity KEEP

Verify:

- Input snapshot, buffered input, and input locks are not changed by transition policy.
- Horizontal and vertical velocity are not changed by allowed or blocked transition decisions.
- Grounded/airborne branch does not alter velocity.

### 6. Hysteresis, High-Speed, and Airborne Entry

Verify:

- Hysteresis margin/stability rule prevents boundary ping-pong.
- Reverse transition is possible only after crossing back beyond the hysteresis rule.
- High-speed boundary crossing emits at most one target-room transition.
- Airborne crossing uses the same policy as grounded crossing.
- Any sample-based limitation is documented and non-blocking if tests cover the locked course behavior.

### 7. Regression Tests

Run:

```text
Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Expected minimum tests: 76
Expected result: PASS
PlayMode: NOT RUN unless needed for diagnosis
```

If audit finds semantic invalidity even with passing tests, report FAIL or BLOCKED according to evidence.

### 8. Scope and Dependency Ledger

Verify no out-of-scope change was introduced by CHAR03:

- No Scene, Prefab, Tilemap, inputactions, Packages, ProjectSettings, MapDesign, MAP runtime, authoring CSV, generator, validator, overlay, camera component, Cinemachine, animation, presentation, terrain mutation, item, enemy, bomb, rope, health, HUD, or legacy mutation.
- Live generated-map query/readiness source remains deferred to CHAR06.
- Terrain mutation request remains deferred to CHAR05.
- CHAR04 entry is permitted only if CHAR03 exit is approved.

## Required Report

Write:

```text
CharacterDesign/MCP/REPORTS/CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT_RESULT.md
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
MAP_COORDINATE_AND_QUERY_AUDIT
ROOM_BOUNDARY_READINESS_AUDIT
CAMERA_ROOM_TRANSITION_AUDIT
INPUT_VELOCITY_KEEP_AUDIT
HYSTERESIS_AND_EDGE_ENTRY_AUDIT
DEPENDENCY_DIRECTION
SCOPE_VALIDATION
DEPENDENCY_LEDGER
OUT_OF_SCOPE_FINDINGS
CHAR03 EXIT
CHAR04_01 ENTRY
DONE CONDITIONS
NEXT
```

If all gates pass, include exactly:

```text
CHAR03 EXIT: APPROVED
CHAR04_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH
```

If any gate fails, include exactly:

```text
CHAR03 EXIT: REJECTED
CHAR04_01 ENTRY: BLOCKED
```

## Done Conditions

All done conditions must be checked in the report:

- [ ] CHAR03_01 PASS report verified.
- [ ] CHAR03_02 PASS report verified.
- [ ] Current status chain verified.
- [ ] Source registry marker/hash verified.
- [ ] MAP coordinate bridge and query contract audited.
- [ ] Dependency direction audited.
- [ ] Room boundary readiness gate audited.
- [ ] Camera-room transition policy audited.
- [ ] Input KEEP and velocity KEEP audited.
- [ ] Hysteresis, high-speed entry, and airborne entry audited.
- [ ] Character EditMode tests pass with at least 76 tests.
- [ ] Scope validation completed.
- [ ] Dependency ledger completed.
- [ ] CHAR03 EXIT decision recorded with exact required text.
- [ ] CHAR04_01 ENTRY decision recorded with exact required text.
- [ ] No code/test/status/master modifications made by this task.
- [ ] CHAR04_01 remains locked.

## Completion Rule

If STATUS is PASS:

- Finalize CHAR03_03 to COMPLETE.
- Set Current Task after finalize to NONE.
- Keep `CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW` locked.
- Do not auto-open CHAR04_01.

If STATUS is FAIL or BLOCKED:

- Keep CHAR03_03 CURRENT.
- Do not open CHAR04_01.

The NEXT section must include:

```text
Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
```

only when PASS/finalized. If not PASS, state why the task remains CURRENT.
