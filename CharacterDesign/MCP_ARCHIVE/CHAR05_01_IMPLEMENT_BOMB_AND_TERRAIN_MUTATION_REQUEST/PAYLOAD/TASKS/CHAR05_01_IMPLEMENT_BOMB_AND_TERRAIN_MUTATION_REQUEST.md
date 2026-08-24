# TASK: CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST

```yaml
task_id: CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST
phase: CHAR05_EQUIPMENT_SURVIVAL_AND_RUN
task_type: IMPLEMENTATION
created: 2026-08-24
workflow: MCP_INBOX_PATCH_ONLY
write_scope: LIMITED_BOMB_EQUIPMENT_RUNTIME_AND_TESTS
```

## Objective

Implement the pure bomb equipment and explosion request contract.

This task owns:

```text
bomb placement eligibility request
bomb spend request, not inventory mutation
bomb fuse countdown model
explosion request when fuse reaches zero
deterministic explosion affected-cell enumeration
terrain mutation request for destructible cells only
enemy/player explosion damage candidates as requests only
no direct Tilemap, MAP runtime, health, HP, death, score, prefab, scene, or presentation mutation
```

This task must not implement rope, health/life application, player death, run failure, HUD, inventory UI, live physics wiring, prefab spawning, scene objects, or actual MAP terrain mutation.

## Entry Gate

Before changing anything, verify:

```text
Current Task: TASKS/CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST.md
CHAR04_04 Result: PASS
CHAR04_04 Result SHA-256: fc0fde8fc75d170f6eafd8436f5e21fb49b2b2b2990fba1bcb75c47ba5b38ab2
CHAR04_04 contains: CHAR04_EXIT_DECISION: APPROVED
CHAR04_04 contains: Current Task after finalize: NONE
CHAR04_04 contains: CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST
CHAR04_04 contains: LOCKED 유지
Source Registry marker: REGISTRY_STATE: FILLED_BY_CHAR00_01
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR05_02 and later tasks: LOCKED
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
10. `CharacterDesign/MCP/TASKS/CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT.md`
11. `CharacterDesign/MCP/REPORTS/CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT_RESULT.md`
12. `CharacterDesign/MCP/REPORTS/CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW_RESULT.md`
13. `CharacterDesign/MCP/REPORTS/CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE_RESULT.md`
14. `CharacterDesign/MCP/REPORTS/CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK_RESULT.md`
15. `CharacterDesign/01_FIXED_SPEC/04_CHARACTER_INTERACTION_RULES.md`
16. `CharacterDesign/01_FIXED_SPEC/05_CHARACTER_COMBAT_RULES.md`
17. `CharacterDesign/01_FIXED_SPEC/06_CHARACTER_EQUIPMENT_SURVIVAL_RULES.md`
18. `CharacterDesign/01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
19. `CharacterDesign/04_TEST_FIXTURES/COMBAT_COURSE_SPEC.md`
20. Current character runtime under `Assets/_Game/Character/Runtime/`
21. Current character EditMode tests under `Assets/_Game/Tests/EditMode/Character/`
22. MAP public coordinate/query/mutation contract from source registry only
23. Legacy bomb/explosion/terrain examples for read-only reference only:
    - `Assets/_Legacy/StarNight/Scripts/Runtime/Player/**`
    - `Assets/_Legacy/StarNight/Scripts/Runtime/Objects/**`
    - `Assets/_Legacy/_Game/Player/Tests/**`
    - `Assets/_Legacy/_Game/Interaction/**`

Do not read or start any `CHAR05_02`, `CHAR05_03`, `CHAR05_04`, `CHAR05_05`, or `CHAR06` task body.

## Allowed Writes

Allowed runtime writes:

```text
Assets/_Game/Character/Runtime/Equipment/**
```

Allowed test writes:

```text
Assets/_Game/Tests/EditMode/Character/Equipment/**
```

Conditional combat writes:

```text
Assets/_Game/Character/Runtime/Combat/**
Assets/_Game/Tests/EditMode/Character/Combat/**
```

Use conditional combat writes only if a shared request type must interoperate with existing CHAR04 damage or impact candidates. Do not rewrite stomp, contact, carry, throw, or impact behavior.

Required report:

```text
CharacterDesign/MCP/REPORTS/CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST_RESULT.md
```

Forbidden:

- Runtime or test changes outside allowed Equipment paths and conditional Combat bridge paths.
- asmdef changes unless compile proves the new files are not included by existing `Game.Character.Runtime` or `Game.Character.Tests.EditMode`; if asmdef change is unavoidable, BLOCK and report instead.
- Scene, prefab, physics layer asset, inputactions, Packages, ProjectSettings, MapDesign, MAP runtime, Tilemap, camera, animation, presentation, or legacy code changes.
- Actual MAP terrain mutation, direct Tilemap writes, tile asset changes, or chunk/map regeneration.
- Health/life deduction, enemy HP mutation, death, score, run failure, HUD, inventory UI, item pickup tables, or save data changes.
- Rope behavior.
- Adding a basic attack, melee, shoot, dash, wall jump, or double jump.
- Adding ActionId values beyond the existing locked set.
- Animator-event-owned bomb, explosion, terrain, or damage authority.
- Opening or installing any future task.
- Editing `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md` or `CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md` during task execution.

## Required Implementation

### 1. Bomb Placement Request

Implement pure bomb placement eligibility and request values.

Required behavior:

```text
placement input records actor id, intended cell/world position, available bomb count snapshot, and placement query result
available bomb count > 0 and target cell placeable -> placement request + bomb spend request
available bomb count <= 0 -> no placement request and no spend request
blocked, occupied, or out-of-bounds target cell -> no placement request and no spend request
placement request does not instantiate a prefab or mutate inventory
```

The policy must accept an input snapshot. It must not wire live input or add ActionId values.

### 2. Bomb Fuse and Explosion Request

Implement a pure bomb fuse model.

Required behavior:

```text
bomb state records bomb id, owner id, position, remaining fuse seconds, and configured explosion radius
positive remaining fuse -> no explosion request
fuse reaches zero or below -> exactly one explosion request
explosion request records center, radius, owner/source, and configured damage amount
timer logic is deterministic and clamp-safe
```

This task may represent the request that an object should explode. It must not spawn effects, destroy objects, or play animation.

### 3. Terrain Mutation Request

Implement terrain mutation request generation from explosion geometry.

Required behavior:

```text
explosion affected cells are deterministic and deduplicated
only cells within configured radius are considered
only destructible terrain cells produce terrain mutation requests
solid indestructible, empty, occupied non-terrain, and out-of-bounds cells are skipped
request records cell coordinate and mutation intent only
request does not mutate MAP, Tilemap, or tile assets
```

Use the MAP public coordinate/query/mutation contract from the source registry only. If a direct MAP contract type cannot be referenced without asmdef or dependency violation, create a character-side request DTO that is explicitly compatible by fields and document the bridge in the report.

### 4. Explosion Damage Candidates

Implement explosion damage candidate requests.

Required behavior:

```text
enemy within explosion radius -> enemy explosion damage candidate
player within explosion radius -> player explosion damage candidate
targets outside radius -> no damage candidate
damage candidate records target id, source bomb/explosion id, amount, and direction from center
damage candidates do not apply health, life, HP, stun, removal, death, score, knockback, or presentation
```

Actual health and death handling is deferred to CHAR05_03.

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
BombPlacement_AvailableBombCreatesPlacementAndSpendRequest
BombPlacement_NoAvailableBombCreatesNoPlacement
BombPlacement_BlockedOrOutOfBoundsCellRefusesPlacement
BombFuse_PositiveRemainingTimeCreatesNoExplosion
BombFuse_ReachesZeroCreatesSingleExplosionRequest
Explosion_TerrainMutationRequestIncludesOnlyDestructibleCells
Explosion_TerrainMutationRequestIsDeterministicAndDeduplicated
Explosion_IndestructibleEmptyAndOutOfBoundsCellsAreSkipped
Explosion_EnemyAndPlayerTargetsWithinRadiusCreateDamageCandidates
Explosion_TargetsOutsideRadiusCreateNoDamageCandidate
Explosion_DamageCandidatesAndTerrainRequestsDoNotApplySideEffects
BombRuntime_DoesNotUseAnimatorPhysicsTilemapOrForbiddenActions
```

Names may vary if they fit existing conventions, but the report must map actual test names to these twelve required behaviors.

Run:

```text
Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Expected minimum tests: 122
Expected result: PASS
```

The expected minimum is previous 110 plus at least 12 CHAR05_01 tests.

PlayMode is not required for this task.

## Required Report

Write:

```text
CharacterDesign/MCP/REPORTS/CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST_RESULT.md
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
BOMB_PLACEMENT
BOMB_FUSE_AND_EXPLOSION
TERRAIN_MUTATION_REQUEST
EXPLOSION_DAMAGE_CANDIDATES
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

- [ ] CHAR04 EXIT approved and CHAR04_04 PASS/hash verified.
- [ ] Source registry marker/hash verified.
- [ ] Available bomb and valid target produce placement request.
- [ ] No bomb count or invalid target refuses placement.
- [ ] Placement emits spend request but does not mutate inventory.
- [ ] Positive fuse produces no explosion.
- [ ] Fuse zero or below produces exactly one explosion request.
- [ ] Explosion affected-cell enumeration is deterministic and deduplicated.
- [ ] Only destructible cells produce terrain mutation requests.
- [ ] Terrain mutation request does not mutate MAP, Tilemap, or tile assets.
- [ ] Enemy/player targets inside radius produce damage candidates.
- [ ] Targets outside radius produce no damage candidate.
- [ ] Damage candidates do not apply health, life, HP, stun, removal, death, score, knockback, or presentation.
- [ ] Animator events and physics callbacks are not authority.
- [ ] Forbidden basic attack/movement features remain absent.
- [ ] ActionId locked set remains unchanged.
- [ ] Character EditMode tests pass with at least 122 tests.
- [ ] Unity compile errors 0.
- [ ] Scope validation completed.
- [ ] CHAR05_02 remains locked.

## Completion Rule

If STATUS is PASS:

- Finalize CHAR05_01 to COMPLETE.
- Set Current Task after finalize to NONE.
- Keep `CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT` locked.
- Do not auto-open CHAR05_02.

If STATUS is FAIL or BLOCKED:

- Keep CHAR05_01 CURRENT.
- Do not open CHAR05_02.

The NEXT section must include:

```text
Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
```

only when PASS/finalized. If not PASS, state why the task remains CURRENT.
