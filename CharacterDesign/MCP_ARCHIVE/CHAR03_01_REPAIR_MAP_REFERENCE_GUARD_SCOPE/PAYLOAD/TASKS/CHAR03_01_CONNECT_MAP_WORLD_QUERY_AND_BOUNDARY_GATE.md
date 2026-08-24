# TASK: CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE

```yaml
task_id: CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE
phase: CHAR03_MAP_ROOM_INTEGRATION
task_type: CHANGE_CONTROL_REPAIR_AND_IMPLEMENTATION
revision: repair_map_reference_guard_scope
created: 2026-08-23
workflow: MCP_INBOX_PATCH_ONLY
write_scope: LIMITED_MAP_INTEGRATION_RUNTIME_TESTS_AND_DEPENDENCY_GUARD_REPAIR
```

## Objective

Resolve the CHAR03_01 BLOCKED write-scope conflict, then implement the MAP coordinate/query contract connection and room-boundary readiness gate.

The blocker is specific:

```text
CHAR03_01 requires Game.Character.Runtime.asmdef to reference Game.Map.Runtime.
Existing CharacterGroundProbeTests.cs has a CHAR01-era guard that forbids all Game.Map* references.
The old guard is now obsolete for CHAR03 but the original task did not allow editing that test file.
```

This repair revision expands the write allowlist for exactly one existing test file so the dependency guard can be updated instead of bypassed.

Do not open CHAR03_02. CHAR03_02 remains locked until this task produces a PASS report and finalizes to Current Task NONE.

## Entry Gate

Before changing anything, verify:

```text
Current Task: TASKS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE.md
Previous CHAR03_01 Result: BLOCKED
Previous CHAR03_01 Result SHA-256: b4e37ef7dd56fc1a081969619ace9b25b4edd62f9cef4167cd0bd88ded9e963f
Previous CHAR03_01 contains: GroundProbe_DoesNotRequireMapOrTilemapTypes
Previous CHAR03_01 contains: write-scope 충돌
CHAR02_03 Result: PASS
CHAR02 EXIT: APPROVED
CHAR03_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH
CHAR02_03 Result SHA-256: e118ac9d286252bad58387e2675b32d6eee38abf7f592ecb06b6d591d6370fb5
Source Registry marker: REGISTRY_STATE: FILLED_BY_CHAR00_01
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR03_02 and later tasks: LOCKED
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
12. `CharacterDesign/MCP/TASKS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT.md`
13. `CharacterDesign/MCP/REPORTS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT_RESULT.md`
14. `CharacterDesign/01_FIXED_SPEC/06_CHARACTER_MAP_INTEGRATION_RULES.md`
15. `CharacterDesign/01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
16. `CharacterDesign/04_TEST_FIXTURES/ROOM_TRANSITION_COURSE_SPEC.md`
17. Current character runtime under `Assets/_Game/Character/Runtime/`
18. Current character EditMode tests under `Assets/_Game/Tests/EditMode/Character/`
19. `Assets/_Game/Tests/EditMode/Character/CharacterGroundProbeTests.cs`
20. MAP public coordinate/domain runtime under `Assets/_Game/Map/Runtime/WorldGeneration/Domain/`
21. MAP tile layer public model under `Assets/_Game/Map/Runtime/WorldGeneration/`
22. MAP EditMode asmdef and coordinate tests for convention only.

Do not read or start any `CHAR03_02`, `CHAR03_03`, `CHAR04`, `CHAR05`, or `CHAR06` task body.

## Allowed Writes

Allowed runtime writes:

```text
Assets/_Game/Character/Runtime/MapIntegration/**
Assets/_Game/Character/Runtime/Game.Character.Runtime.asmdef
```

The asmdef may be changed only to add the existing `Game.Map.Runtime` reference required by MAP public coordinate/domain use. Do not add a new asmdef.

Allowed test writes:

```text
Assets/_Game/Tests/EditMode/Character/MapIntegration/**
Assets/_Game/Tests/EditMode/Character/Game.Character.Tests.EditMode.asmdef
Assets/_Game/Tests/EditMode/Character/CharacterGroundProbeTests.cs
```

`CharacterGroundProbeTests.cs` may be changed only to repair the obsolete CHAR01-era dependency guard. Do not weaken the guard into a broad allowance.

Required guard update:

```text
Allowed after CHAR03_01: Game.Map.Runtime exactly.
Still forbidden: UnityEngine.TilemapModule, Unity.InputSystem, MapAuthoring/editor/test assemblies, legacy assemblies, Game.Stage.Runtime, any Game.Map.* assembly other than Game.Map.Runtime unless already present and justified.
```

Conditional MAP runtime writes:

```text
Assets/_Game/Map/Runtime/WorldGeneration/PublicContracts/**
```

Use this path only if the current MAP runtime has no public read-only query/readiness contract that character code can consume. If used, keep it to pure contracts/value models only. Do not change generation algorithms, authoring CSV, validators, overlays, Tilemap, scenes, prefabs, or MapDesign documents.

Required report:

```text
CharacterDesign/MCP/REPORTS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE_RESULT.md
```

Forbidden:

- Tilemap direct dependency from character runtime.
- Direct reference to MicroChunk generator internals from character runtime.
- Duplicating MAP coordinate conversion math in character runtime.
- Camera transition implementation.
- Hysteresis implementation.
- Input suppression, input rewrite, or velocity rewrite.
- Terrain mutation request implementation.
- Generated map route integration.
- Scene, prefab, inputactions, Packages, ProjectSettings, MapDesign, or legacy code changes.
- Opening or installing any future task.
- Editing `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md` or `CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md` during task execution.

## Required Repair and Implementation

### 1. Dependency Guard Repair

Update the obsolete CHAR01-era dependency guard so it reflects CHAR03-approved dependency direction.

The repaired guard must prove:

- `Game.Character.Runtime` may reference `Game.Map.Runtime`.
- `Game.Character.Runtime` must not reference Tilemap modules.
- `Game.Character.Runtime` must not reference InputSystem.
- `Game.Character.Runtime` must not reference MAP authoring/editor/test assemblies.
- `Game.Character.Runtime` must not reference legacy assemblies.
- `Game.Character.Runtime` must not reference stale `Game.Stage.Runtime`.
- The MAP dependency is one-way: Character may consume MAP public runtime; MAP runtime must not reference Character runtime.

Do not delete dependency-direction coverage. Rename the test if needed so it no longer claims "does not require Map" after CHAR03.

### 2. MAP Coordinate Bridge

Implement a character-side bridge that uses MAP public coordinate utilities instead of duplicating coordinate math.

Required behavior:

```text
world position -> MAP world tile coordinate
MAP world tile coordinate -> world position or cell center
valid map bounds accepted
out-of-bounds coordinates rejected without clamping
cell size remains 1 world unit
```

The report must name the MAP public APIs used.

### 3. Character MAP World Query Contract

Implement a read-only character-facing world query surface that can answer at least:

```text
is solid
is one-way
is hazard
is liquid
is breakable
is empty/passable
```

Use MAP public domain concepts for cell coordinates and layer/tile meaning. Do not let character runtime read Tilemap, scene objects, CSV authoring rows, microchunk placement internals, or generator passes directly.

If no live generated-map data source exists yet, implement a pure contract plus deterministic fake-backed tests. Record the live generated-map data source as deferred to CHAR06, not as a CHAR03_01 failure, as long as the character-facing contract and MAP public coordinate dependency are correct.

### 4. Room Boundary Readiness Gate

Implement a gate that decides whether the character may cross a room boundary based on target room readiness.

Required behavior:

```text
prepared destination room -> crossing allowed
unprepared destination room -> crossing blocked
missing destination room -> crossing blocked
current-room interior movement -> unaffected
allowed or blocked decision does not mutate input snapshot
allowed or blocked decision does not mutate velocity
```

The gate returns a decision only. It must not move the camera, snap the player, suppress input, rewrite velocity, or apply hysteresis. CHAR03_02 owns camera transition, KEEP policy application details, and hysteresis.

## Required Test Coverage

Add deterministic EditMode tests covering at least these behaviors:

```text
DependencyGuard_AllowsOnlyGameMapRuntimeAndRejectsTilemapAuthoringLegacy
CoordinateBridge_UsesMapWorldCoordinateUtility
CoordinateBridge_RejectsOutOfBoundsWithoutClamping
MapWorldQuery_ReportsSolidHazardOneWayLiquidBreakableAndEmpty
MapWorldQuery_DoesNotUseTilemapOrMicroChunkInternals
BoundaryGate_BlocksUnpreparedDestinationRoom
BoundaryGate_BlocksMissingDestinationRoom
BoundaryGate_AllowsPreparedDestinationRoom
BoundaryGate_DoesNotMutateInputOrVelocity
```

Names may vary if they fit existing conventions, but the report must map actual test names to these nine required behaviors.

Run:

```text
Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Expected minimum tests: 66
Expected result: PASS
```

The expected minimum is previous 57 plus at least 8 original CHAR03_01 tests plus the repaired dependency guard coverage.

If conditional MAP public contract files are added, also run `Game.Map.Tests.EditMode` or explain with compile evidence why no MAP test assembly execution was required.

PlayMode is not required for this task.

## Required Report

Overwrite or replace:

```text
CharacterDesign/MCP/REPORTS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE_RESULT.md
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
CHANGE_CONTROL
MAP_COORDINATE_BRIDGE
MAP_WORLD_QUERY_CONTRACT
ROOM_BOUNDARY_READINESS_GATE
DEPENDENCY_DIRECTION
SCOPE_VALIDATION
DEPENDENCY_LEDGER
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
```

## Done Conditions

All done conditions must be checked in the report:

- [ ] Previous CHAR03_01 BLOCKED report verified with exact SHA and blocker text.
- [ ] CHAR02_03 PASS and CHAR02 EXIT approved verified.
- [ ] Source registry marker/hash verified.
- [ ] Obsolete map-reference guard repaired, not deleted.
- [ ] Character runtime references MAP public runtime only as approved.
- [ ] Coordinate bridge uses MAP coordinate utility and rejects out-of-bounds without clamping.
- [ ] Character-facing world query contract covers solid, one-way, hazard, liquid, breakable, and empty/passable.
- [ ] Room boundary gate blocks unprepared and missing destinations.
- [ ] Room boundary gate allows prepared destinations.
- [ ] Boundary decision does not mutate input or velocity.
- [ ] No camera transition or hysteresis implementation.
- [ ] No Tilemap, scene, prefab, inputactions, Packages, ProjectSettings, MapDesign, or legacy mutation.
- [ ] Character EditMode tests pass with at least 66 tests.
- [ ] Unity compile errors 0.
- [ ] CHAR03_02 remains locked.

## Completion Rule

If STATUS is PASS:

- Finalize CHAR03_01 to COMPLETE.
- Set Current Task after finalize to NONE.
- Keep `CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY` locked.
- Do not auto-open CHAR03_02.

If STATUS is FAIL or BLOCKED:

- Keep CHAR03_01 CURRENT.
- Do not open CHAR03_02.

The NEXT section must include:

```text
Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
```

only when PASS/finalized. If not PASS, state why the task remains CURRENT.
