# TASK: CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT

```yaml
task_id: CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT
phase: CHAR05_EQUIPMENT_SURVIVAL_AND_RUN
task_type: IMPLEMENTATION
created: 2026-08-24
workflow: MCP_INBOX_PATCH_ONLY
write_scope: LIMITED_ROPE_EQUIPMENT_TRAVERSAL_RUNTIME_AND_TESTS
```

## Objective

Implement the pure rope equipment and traversal support contract.

This task owns:

```text
rope placement eligibility request
rope spend request, not inventory mutation
deterministic vertical rope segment generation
rope segment generation constrained by MAP bounds, solid blockers, and max length
rope climb overlap/intent detection
rope climb motor request for up/down traversal
top/bottom rope bounds clamp
no prefab, scene, Tilemap, MAP mutation, live physics wiring, animation, HUD, or inventory UI
```

This task must not implement health/life application, hazards, player death, run failure, HUD, rope prefab spawning, scene objects, physics layers, or future integration wiring.

## Entry Gate

Before changing anything, verify:

```text
Current Task: TASKS/CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT.md
CHAR05_01 Result: PASS
CHAR05_01 Result SHA-256: 1c5036404d957cc5ca534d4c0ec89e77995c3d6adfa66306b73915bc42005e7f
CHAR05_01 contains: Current Task after finalize: NONE
CHAR05_01 contains: CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT
CHAR05_01 contains: LOCKED 유지
Source Registry marker: REGISTRY_STATE: FILLED_BY_CHAR00_01
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR05_03 and later tasks: LOCKED
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
10. `CharacterDesign/MCP/TASKS/CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST.md`
11. `CharacterDesign/MCP/REPORTS/CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST_RESULT.md`
12. `CharacterDesign/MCP/REPORTS/CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT_RESULT.md`
13. `CharacterDesign/01_FIXED_SPEC/01_CHARACTER_GAMEPLAY_RULES.md`
14. `CharacterDesign/01_FIXED_SPEC/02_CHARACTER_INPUT_RULES.md`
15. `CharacterDesign/01_FIXED_SPEC/06_CHARACTER_MAP_INTEGRATION_RULES.md`
16. `CharacterDesign/01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
17. `CharacterDesign/03_DATA_SCHEMA/CHARACTER_INVENTORY_SCHEMA.md`
18. `CharacterDesign/03_DATA_SCHEMA/CHARACTER_ACTION_SCHEMA.md`
19. `CharacterDesign/04_TEST_FIXTURES/COMBAT_COURSE_SPEC.md`
20. Current character runtime under `Assets/_Game/Character/Runtime/`
21. Current character EditMode tests under `Assets/_Game/Tests/EditMode/Character/`
22. MAP public coordinate/query contract from source registry only
23. Legacy rope/climb examples for read-only reference only:
    - `Assets/_Legacy/StarNight/Scripts/Runtime/Player/**`
    - `Assets/_Legacy/StarNight/Scripts/Runtime/Objects/**`
    - `Assets/_Legacy/_Game/Player/Tests/**`
    - `Assets/_Legacy/_Game/Interaction/**`

Do not read or start any `CHAR05_03`, `CHAR05_04`, `CHAR05_05`, or `CHAR06` task body.

## Allowed Writes

Allowed runtime writes:

```text
Assets/_Game/Character/Runtime/Equipment/**
Assets/_Game/Character/Runtime/Traversal/**
```

Allowed test writes:

```text
Assets/_Game/Tests/EditMode/Character/Equipment/**
Assets/_Game/Tests/EditMode/Character/Traversal/**
```

Conditional movement writes:

```text
Assets/_Game/Character/Runtime/Movement/**
Assets/_Game/Tests/EditMode/Character/Movement/**
```

Use conditional movement writes only if a small request adapter is required to expose rope climb motor output to existing movement contracts. Do not rewrite ground, jump, air control, collision query, or room transition behavior.

Required report:

```text
CharacterDesign/MCP/REPORTS/CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT_RESULT.md
```

Forbidden:

- Runtime or test changes outside allowed Equipment/Traversal paths and conditional Movement bridge paths.
- asmdef changes unless compile proves the new files are not included by existing `Game.Character.Runtime` or `Game.Character.Tests.EditMode`; if asmdef change is unavoidable, BLOCK and report instead.
- Scene, prefab, physics layer asset, inputactions, Packages, ProjectSettings, MapDesign, MAP runtime, Tilemap, camera, animation, presentation, or legacy code changes.
- Actual rope prefab spawning, object instantiation, Tilemap changes, or MAP mutation.
- Health/life deduction, hazards, death, run failure, HUD, inventory UI, item pickup tables, or save data changes.
- Bomb behavior changes.
- Adding a basic attack, melee, shoot, dash, wall jump, or double jump.
- Adding ActionId values beyond the existing locked set.
- Animator-event-owned rope or climb authority.
- Unity physics callback-owned rope or climb authority.
- Opening or installing any future task.
- Editing `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md` or `CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md` during task execution.

## Required Implementation

### 1. Rope Placement Request

Implement pure rope placement eligibility and request values.

Required behavior:

```text
placement input records actor id, origin cell/world position, available rope count snapshot, and placement query result
available rope count > 0 and origin is valid/placeable -> rope placement request + rope spend request
available rope count <= 0 -> no placement request and no spend request
invalid, blocked, occupied, or out-of-bounds origin -> no placement request and no spend request
placement request does not instantiate a rope prefab or mutate inventory
```

The policy must accept an input snapshot. It must not wire live input or add ActionId values.

### 2. Rope Segment Generation

Implement deterministic vertical rope segment request generation.

Required behavior:

```text
rope segments are generated from origin upward along one cell column
segment count is capped by centralized max rope length
MAP/world bounds stop generation
solid blocker cells stop generation before entering the blocker
passable cells produce rope segment requests
segment order is deterministic and deduplicated
segment requests do not mutate MAP, Tilemap, scene, prefab, or physics assets
```

Use MAP public coordinate/query contracts from the source registry only.

### 3. Rope Climb Traversal Request

Implement pure rope climb detection and motor request values.

Required behavior:

```text
player overlap/near rope segment + climb intent -> climb motor request
no rope overlap or no climb intent -> no climb motor request
up input produces positive vertical climb velocity
down input produces negative vertical climb velocity
no vertical input may hold position or produce zero climb velocity, according to existing fixed rules
climb request is a request/value object and does not mutate player state or velocity directly
```

Do not rewrite existing movement state machine or jump logic. If the existing state model needs a bridge, implement only a minimal request adapter and document it.

### 4. Rope Boundary Rules

Implement bounds for rope traversal.

Required behavior:

```text
climb target is clamped to rope top and bottom segment bounds
player cannot climb beyond generated rope extent
player cannot climb beyond MAP/world bounds
rope traversal does not grant wall jump, dash, double jump, or extra air control
```

### 5. Authority and Forbidden Feature Guard

Keep decision authority pure.

Required behavior:

```text
no Animator event authority
no Unity physics callback authority
no direct MAP or Tilemap mutation
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
RopePlacement_AvailableRopeCreatesPlacementAndSpendRequest
RopePlacement_NoRopeCreatesNoPlacement
RopePlacement_BlockedOrOutOfBoundsOriginRefusesPlacement
RopeSegments_GenerateVerticalCellsUntilBlockedOrMaxLength
RopeSegments_AreDeterministicOrderedAndDeduplicated
RopeSegments_DoNotMutateMapOrTilemap
RopeClimb_OverlapAndClimbIntentCreatesMotorRequest
RopeClimb_NoOverlapOrNoIntentCreatesNoMotorRequest
RopeClimb_UpDownInputProducesVerticalVelocity
RopeClimb_TopAndBottomBoundsClampTraversal
RopeRuntime_DoesNotUseAnimatorPhysicsTilemapOrForbiddenActions
RopeRuntime_DoesNotIntroduceDashWallJumpDoubleJumpOrBasicAttack
```

Names may vary if they fit existing conventions, but the report must map actual test names to these twelve required behaviors.

Run:

```text
Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Expected minimum tests: 134
Expected result: PASS
```

The expected minimum is previous 122 plus at least 12 CHAR05_02 tests.

PlayMode is not required for this task.

## Required Report

Write:

```text
CharacterDesign/MCP/REPORTS/CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT_RESULT.md
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
ROPE_PLACEMENT
ROPE_SEGMENT_GENERATION
ROPE_CLIMB_TRAVERSAL
ROPE_BOUNDARY_RULES
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

- [ ] CHAR05_01 PASS/hash verified.
- [ ] Source registry marker/hash verified.
- [ ] Available rope and valid origin produce placement request.
- [ ] No rope count or invalid origin refuses placement.
- [ ] Placement emits spend request but does not mutate inventory.
- [ ] Rope segment generation is vertical, deterministic, and deduplicated.
- [ ] Max rope length caps generated segments.
- [ ] Bounds and solid blockers stop segment generation.
- [ ] Rope segment request does not mutate MAP, Tilemap, scene, prefab, or physics assets.
- [ ] Rope overlap plus climb intent creates climb motor request.
- [ ] No overlap or no intent creates no climb motor request.
- [ ] Up/down input produces clamped vertical climb request.
- [ ] Top/bottom rope bounds clamp traversal.
- [ ] Rope traversal does not grant wall jump, dash, double jump, or extra air control.
- [ ] Animator events and physics callbacks are not authority.
- [ ] Forbidden basic attack/movement features remain absent.
- [ ] ActionId locked set remains unchanged.
- [ ] Character EditMode tests pass with at least 134 tests.
- [ ] Unity compile errors 0.
- [ ] Scope validation completed.
- [ ] CHAR05_03 remains locked.

## Completion Rule

If STATUS is PASS:

- Finalize CHAR05_02 to COMPLETE.
- Set Current Task after finalize to NONE.
- Keep `CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE` locked.
- Do not auto-open CHAR05_03.

If STATUS is FAIL or BLOCKED:

- Keep CHAR05_02 CURRENT.
- Do not open CHAR05_03.

The NEXT section must include:

```text
Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
```

only when PASS/finalized. If not PASS, state why the task remains CURRENT.
