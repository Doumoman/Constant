# TASK: CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK

```yaml
task_id: CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK
phase: CHAR04_INTERACTION_AND_COMBAT
task_type: IMPLEMENTATION
created: 2026-08-24
workflow: MCP_INBOX_PATCH_ONLY
write_scope: LIMITED_IMPACT_CONTRACT_RUNTIME_AND_TESTS
```

## Objective

Implement the pure contract for thrown-object and solid-world impact decisions, and prove that the character system still has no separate basic attack.

This task owns:

```text
moving thrown object + eligible enemy target -> enemy impact damage candidate
moving thrown object + solid world target -> object stop/rest request
owner collision grace active -> owner/self impact suppressed
stationary or below-threshold object -> no impact event
impact result separates source object request, enemy damage candidate, and player damage candidate
no basic attack / melee / shoot / dash / wall jump / double jump feature is introduced
```

This task must not implement health deduction, enemy HP, death, score, item durability, terrain mutation, bomb, rope, HUD, prefab wiring, scene objects, physics layer assets, or animation/presentation.

## Entry Gate

Before changing anything, verify:

```text
Current Task: TASKS/CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK.md
CHAR04_02 Result: PASS
CHAR04_02 Result SHA-256: e68259585ed2cfd4ec4baf01cccb732dc073f3a372551725e1c8c185e4d0366f
CHAR04_02 contains: Current Task after finalize: NONE
CHAR04_02 contains: CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK
CHAR04_02 contains: LOCKED 유지
Source Registry marker: REGISTRY_STATE: FILLED_BY_CHAR00_01
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR04_04 and later tasks: LOCKED
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
10. `CharacterDesign/MCP/TASKS/CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW.md`
11. `CharacterDesign/MCP/REPORTS/CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW_RESULT.md`
12. `CharacterDesign/MCP/TASKS/CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE.md`
13. `CharacterDesign/MCP/REPORTS/CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE_RESULT.md`
14. `CharacterDesign/01_FIXED_SPEC/04_CHARACTER_INTERACTION_RULES.md`
15. `CharacterDesign/01_FIXED_SPEC/05_CHARACTER_COMBAT_RULES.md`
16. `CharacterDesign/01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
17. `CharacterDesign/04_TEST_FIXTURES/INTERACTION_COURSE_SPEC.md`
18. `CharacterDesign/04_TEST_FIXTURES/COMBAT_COURSE_SPEC.md`
19. Current character runtime under `Assets/_Game/Character/Runtime/`
20. Current character tests under `Assets/_Game/Tests/EditMode/Character/`
21. Legacy throw/impact/contact examples for read-only reference only:
    - `Assets/_Legacy/StarNight/Scripts/Runtime/Player/**`
    - `Assets/_Legacy/StarNight/Scripts/Runtime/Objects/**`
    - `Assets/_Legacy/_Game/Player/Tests/**`
    - `Assets/_Legacy/_Game/Interaction/**`

Do not read or start any `CHAR04_04`, `CHAR05`, or `CHAR06` task body.

## Allowed Writes

Allowed runtime writes:

```text
Assets/_Game/Character/Runtime/Combat/**
```

Allowed test writes:

```text
Assets/_Game/Tests/EditMode/Character/Combat/**
```

Conditional interaction writes:

```text
Assets/_Game/Character/Runtime/Interaction/**
Assets/_Game/Tests/EditMode/Character/Interaction/**
```

Use conditional interaction writes only if a small adapter is required to consume the existing CHAR04_01 throw request or owner collision grace contract. Do not rewrite carry, drop, throw, or owner grace behavior.

Required report:

```text
CharacterDesign/MCP/REPORTS/CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK_RESULT.md
```

Forbidden:

- Runtime or test changes outside the allowed Combat paths and conditional Interaction bridge paths.
- asmdef changes unless compile proves the new files are not included by existing `Game.Character.Runtime` or `Game.Character.Tests.EditMode`; if asmdef change is unavoidable, BLOCK and report instead.
- Scene, prefab, physics layer asset, inputactions, Packages, ProjectSettings, MapDesign, MAP runtime, Tilemap, camera, animation, presentation, or legacy code changes.
- Health/life deduction, enemy HP, death, score, item durability, terrain mutation, or object destruction side effects.
- Bomb, explosion, rope, run failure, HUD, or presentation implementation.
- Adding a basic attack, melee, shoot, dash, wall jump, or double jump.
- Adding ActionId values beyond the existing locked set.
- Animator-event-owned impact or damage.
- Opening or installing any future task.
- Editing `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md` or `CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md` during task execution.

## Required Implementation

### 1. Impact Source and Target Snapshots

Implement pure value objects for impact decisions.

Required behavior:

```text
impact source records object id, optional owner id, source kind, velocity, and owner grace state
impact target records target kind and optional target id
source speed is compared against a centralized minimum impact speed
stationary or below-threshold source creates no impact event
```

The implementation must be deterministic and must not depend on Unity physics callbacks or Animator events as authority.

### 2. Thrown Object Enemy Impact Contract

Implement thrown-object enemy impact as a request/value contract only.

Required behavior:

```text
moving thrown object + hostile enemy target + no owner grace block -> enemy impact damage candidate
candidate records source object, target enemy, impact direction, and configured amount
candidate does not apply enemy HP, stun, removal, death, score, or presentation
stunned/non-hostile carryable target does not become hostile damage unless explicitly marked hostile
```

CHAR04_02 already owns stomp stun/removal. Do not merge thrown impact with stomp flow.

### 3. Owner Collision Grace

Respect the CHAR04_01 owner collision grace contract.

Required behavior:

```text
owner grace active + owner/self target -> no impact event
owner grace expired + eligible target -> normal impact decision
owner grace logic is centralized and testable
```

Do not rewrite CHAR04_01 throw behavior.

### 4. Solid World Impact Contract

Implement solid-world impact as a source-object request only.

Required behavior:

```text
moving thrown object + solid world target -> object stop/rest request
solid world impact does not mutate terrain
solid world impact does not damage player or enemy by itself
solid world impact does not instantiate effects or presentation
```

Terrain mutation is deferred to CHAR05_01.

### 5. No Basic Attack Guard

Keep the player action surface locked.

Required behavior:

```text
no basic attack
no melee
no shoot
no dash
no wall jump
no double jump
ActionId locked set remains unchanged
```

If a legacy example contains attack-like names, it may be read only as legacy context and must not be copied into active runtime.

## Required Test Coverage

Add deterministic EditMode tests covering at least these behaviors:

```text
Impact_ThrownObjectEnemyTargetCreatesDamageCandidate
Impact_OwnerGraceSuppressesOwnerSelfImpact
Impact_OwnerGraceExpiredAllowsEligibleImpact
Impact_StationaryOrBelowThresholdSourceCreatesNoEvent
Impact_SolidWorldCreatesObjectStopRequestOnly
Impact_ResultSeparatesObjectEnemyAndPlayerRequests
Impact_NonHostileTargetDoesNotCreateEnemyDamageCandidate
Impact_RuntimeDoesNotUseAnimatorEventsAsImpactAuthority
NoBasicAttack_ActionSurfaceRemainsLocked
NoBasicAttack_RuntimeDoesNotIntroduceForbiddenMovementOrAttackFeatures
```

Names may vary if they fit existing conventions, but the report must map actual test names to these ten required behaviors.

Run:

```text
Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Expected minimum tests: 110
Expected result: PASS
```

The expected minimum is previous 100 plus at least 10 CHAR04_03 tests.

PlayMode is not required for this task.

## Required Report

Write:

```text
CharacterDesign/MCP/REPORTS/CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK_RESULT.md
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
IMPACT_SOURCE_TARGET
THROWN_OBJECT_ENEMY_IMPACT
OWNER_GRACE_IMPACT_SUPPRESSION
SOLID_WORLD_IMPACT
NO_BASIC_ATTACK_GUARD
DEPENDENCY_DIRECTION
SCOPE_VALIDATION
DEPENDENCY_LEDGER
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
```

## Done Conditions

All done conditions must be checked in the report:

- [ ] CHAR04_02 PASS verified.
- [ ] Source registry marker/hash verified.
- [ ] Moving thrown object can create enemy impact damage candidate.
- [ ] Owner grace suppresses owner/self impact.
- [ ] Expired owner grace allows eligible impact.
- [ ] Stationary or below-threshold object creates no impact event.
- [ ] Solid world impact creates object stop/rest request only.
- [ ] Impact result separates source object, enemy, and player requests.
- [ ] Non-hostile target does not create enemy damage candidate.
- [ ] Impact candidate does not apply health, HP, removal, death, score, or presentation.
- [ ] Animator events are not impact authority.
- [ ] Forbidden basic attack/movement features remain absent.
- [ ] ActionId locked set remains unchanged.
- [ ] Character EditMode tests pass with at least 110 tests.
- [ ] Unity compile errors 0.
- [ ] Scope validation completed.
- [ ] CHAR04_04 remains locked.

## Completion Rule

If STATUS is PASS:

- Finalize CHAR04_03 to COMPLETE.
- Set Current Task after finalize to NONE.
- Keep `CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT` locked.
- Do not auto-open CHAR04_04.

If STATUS is FAIL or BLOCKED:

- Keep CHAR04_03 CURRENT.
- Do not open CHAR04_04.

The NEXT section must include:

```text
Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
```

only when PASS/finalized. If not PASS, state why the task remains CURRENT.
