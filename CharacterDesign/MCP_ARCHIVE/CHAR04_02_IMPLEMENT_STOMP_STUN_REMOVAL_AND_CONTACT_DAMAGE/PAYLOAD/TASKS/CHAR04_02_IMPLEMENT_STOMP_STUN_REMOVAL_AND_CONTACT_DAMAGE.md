# TASK: CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE

```yaml
task_id: CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE
phase: CHAR04_INTERACTION_AND_COMBAT
task_type: IMPLEMENTATION
created: 2026-08-23
workflow: MCP_INBOX_PATCH_ONLY
write_scope: LIMITED_CONTACT_COMBAT_RUNTIME_AND_TESTS
```

## Objective

Implement player-vs-enemy contact combat for stomp, stun, removal, rebound, and contact damage.

This task owns:

```text
descending top contact -> valid stomp
non-descending top contact -> no stomp
side/bottom enemy contact -> player damage candidate
first stomp on normal small enemy -> stun result
second stomp on stunned small enemy -> removal result
player rebound result separated from enemy result
stunned small enemy remains compatible with CHAR04_01 carry candidate contract
```

This task must not implement thrown-object impact damage, environmental impact, bomb, rope, health/life application, HUD, presentation, actual physics collision layers, prefab wiring, or scene objects.

## Entry Gate

Before changing anything, verify:

```text
Current Task: TASKS/CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE.md
CHAR04_01 Result: PASS
CHAR04_01 Result SHA-256: 115949eb70478f68195b22f9ecfa6d2a2cc73872c69ba53aaf7ff772da26a247
CHAR04_01 contains: Current Task after finalize: NONE
CHAR04_01 contains: CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE
Source Registry marker: REGISTRY_STATE: FILLED_BY_CHAR00_01
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR04_03 and later tasks: LOCKED
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
12. `CharacterDesign/01_FIXED_SPEC/04_CHARACTER_INTERACTION_RULES.md`
13. `CharacterDesign/01_FIXED_SPEC/05_CHARACTER_COMBAT_RULES.md`
14. `CharacterDesign/01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
15. `CharacterDesign/04_TEST_FIXTURES/INTERACTION_COURSE_SPEC.md`
16. `CharacterDesign/04_TEST_FIXTURES/COMBAT_COURSE_SPEC.md`
17. Current character runtime under `Assets/_Game/Character/Runtime/`
18. Current `Assets/_Game/Character/Runtime/Interaction/`
19. Current character EditMode tests under `Assets/_Game/Tests/EditMode/Character/`
20. Legacy combat/contact examples for read-only reference only:
    - `Assets/_Legacy/StarNight/Scripts/Runtime/Player/**`
    - `Assets/_Legacy/StarNight/Scripts/Runtime/Objects/**`
    - `Assets/_Legacy/_Game/Player/Tests/**`
    - `Assets/_Legacy/_Game/Interaction/**`

Do not read or start any `CHAR04_03`, `CHAR04_04`, `CHAR05`, or `CHAR06` task body.

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

Use conditional interaction writes only if needed to connect stunned small enemy eligibility to the existing CHAR04_01 carry candidate contract. Do not rewrite carry/drop/throw behavior.

Required report:

```text
CharacterDesign/MCP/REPORTS/CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE_RESULT.md
```

Forbidden:

- Runtime or test changes outside the allowed Combat paths and conditional Interaction bridge paths.
- asmdef changes unless compile proves the new files are not included by existing `Game.Character.Runtime` or `Game.Character.Tests.EditMode`; if asmdef change is unavoidable, BLOCK and report instead.
- Scene, prefab, physics layer asset, inputactions, Packages, ProjectSettings, MapDesign, MAP runtime, Tilemap, camera, animation, presentation, or legacy code changes.
- Thrown-object impact damage.
- Bomb, explosion, rope, health/life deduction, death, run failure, HUD, or presentation implementation.
- Adding a basic attack, melee, shoot, dash, wall jump, or double jump.
- Animator-event-owned damage.
- Opening or installing any future task.
- Editing `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md` or `CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md` during task execution.

## Required Implementation

### 1. Contact Classification

Implement pure contact classification for player-vs-enemy contact.

Required behavior:

```text
top contact while player vertical velocity is descending -> valid stomp
top contact while rising or stationary -> no stomp
side contact -> player damage candidate
bottom contact -> player damage candidate
separated/non-overlapping contact -> no combat event
contact normal or relative position is classified deterministically
```

The classifier must not depend on Animator events or Unity physics callbacks as authority. Physics callbacks may be consumed later as input, but this task implements deterministic decision logic only.

### 2. Stomp Result and Rebound

Implement stomp result value objects and player rebound request.

Required behavior:

```text
valid stomp returns enemy result and player rebound result separately
rebound vertical velocity is centralized and validated
enemy result does not directly mutate player velocity
player rebound result does not directly mutate enemy state
```

### 3. Enemy Stun and Removal Flow

Implement minimal enemy contact state/value flow for small enemies.

Required behavior:

```text
normal small enemy + first valid stomp -> stunned result
stunned small enemy + second valid stomp -> removed result
non-small or non-stomp contact does not produce this stun/removal flow unless explicitly eligible
stunned small enemy can be represented as a carry candidate for CHAR04_01 contract
```

Do not implement thrown object impact removal in this task. CHAR04_03 owns thrown/environment impact contract.

### 4. Player Contact Damage Candidate

Implement contact damage candidate output.

Required behavior:

```text
side contact with hostile enemy -> player damage candidate
bottom contact with hostile enemy -> player damage candidate
valid stomp top contact -> no player damage candidate
stunned non-hostile carryable contact can be non-damaging if documented and tested
damage candidate is a request/value object, not health deduction
```

Health/life application is deferred to CHAR05.

### 5. Forbidden Feature Guard

Keep forbidden features absent:

```text
no basic attack
no melee
no shoot
no dash
no wall jump
no double jump
```

Do not add ActionId values beyond the existing locked set.

## Required Test Coverage

Add deterministic EditMode tests covering at least these behaviors:

```text
ContactClassifier_DescendingTopContactIsValidStomp
ContactClassifier_RisingOrStationaryTopContactIsNotStomp
ContactClassifier_SideAndBottomContactBecomePlayerDamageCandidate
Stomp_FirstStompOnNormalSmallEnemyProducesStunAndRebound
Stomp_SecondStompOnStunnedSmallEnemyProducesRemoval
Stomp_SeparatesPlayerReboundFromEnemyResult
Stomp_ValidTopContactDoesNotCreatePlayerDamageCandidate
ContactDamage_SideContactCreatesDamageRequestWithoutApplyingHealth
ContactDamage_BottomContactCreatesDamageRequestWithoutApplyingHealth
StunnedSmallEnemy_CanBeExposedAsCarryCandidate
CombatRuntime_DoesNotUseAnimatorEventsAsDamageAuthority
CombatRuntime_DoesNotIntroduceBasicAttackDashWallJumpDoubleJumpOrShoot
```

Names may vary if they fit existing conventions, but the report must map actual test names to these twelve required behaviors.

Run:

```text
Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Expected minimum tests: 100
Expected result: PASS
```

The expected minimum is previous 88 plus at least 12 CHAR04_02 tests.

PlayMode is not required for this task.

## Required Report

Write:

```text
CharacterDesign/MCP/REPORTS/CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE_RESULT.md
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
CONTACT_CLASSIFICATION
STOMP_AND_REBOUND
ENEMY_STUN_REMOVAL_FLOW
PLAYER_CONTACT_DAMAGE
STUNNED_ENEMY_CARRY_BRIDGE
FORBIDDEN_FEATURE_GUARD
DEPENDENCY_DIRECTION
SCOPE_VALIDATION
DEPENDENCY_LEDGER
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
```

## Done Conditions

All done conditions must be checked in the report:

- [ ] CHAR04_01 PASS verified.
- [ ] Source registry marker/hash verified.
- [ ] Descending top contact is a valid stomp.
- [ ] Rising/stationary top contact is not a stomp.
- [ ] Side and bottom contacts create player damage candidates.
- [ ] First valid stomp on normal small enemy produces stun.
- [ ] Second valid stomp on stunned small enemy produces removal.
- [ ] Player rebound result is separate from enemy result.
- [ ] Valid stomp does not create player damage candidate.
- [ ] Damage candidate does not apply health/life deduction.
- [ ] Stunned small enemy can be represented as carry candidate.
- [ ] Animator events are not damage authority.
- [ ] Forbidden basic attack/dash/wall jump/double jump/shoot remain absent.
- [ ] Character EditMode tests pass with at least 100 tests.
- [ ] Unity compile errors 0.
- [ ] Scope validation completed.
- [ ] CHAR04_03 remains locked.

## Completion Rule

If STATUS is PASS:

- Finalize CHAR04_02 to COMPLETE.
- Set Current Task after finalize to NONE.
- Keep `CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK` locked.
- Do not auto-open CHAR04_03.

If STATUS is FAIL or BLOCKED:

- Keep CHAR04_02 CURRENT.
- Do not open CHAR04_03.

The NEXT section must include:

```text
Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
```

only when PASS/finalized. If not PASS, state why the task remains CURRENT.
