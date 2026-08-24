# TASK: CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE

```yaml
task_id: CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE
phase: CHAR05_EQUIPMENT_SURVIVAL_AND_RUN
task_type: IMPLEMENTATION
created: 2026-08-24
workflow: MCP_INBOX_PATCH_ONLY
write_scope: LIMITED_SURVIVAL_RUNTIME_AND_TESTS
```

## Objective

Implement the pure health, hazard, death, and run-failure contract.

This task owns:

```text
health state and damage application policy
uniform damage request consumption from contact, impact, explosion, and hazards
invulnerability suppression
lethal damage -> death request
player death or fatal void hazard -> run failure request
enemy/other actor death does not create player run failure
return/retry destination request as data only
no HUD, scene reload, save mutation, animation, audio, prefab, or presentation mutation
```

This task must not implement run-state HUD, inventory display, save data, checkpoint scene loading, camera, animation, audio, live physics wiring, or future integration behavior.

## Entry Gate

Before changing anything, verify:

```text
Current Task: TASKS/CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE.md
CHAR05_02 Result: PASS
CHAR05_02 Result SHA-256: 940e5cf9909cc55a6562704c530ee7abba2d9638ac52627d9b0146922cb98fef
CHAR05_02 contains: Current Task after finalize: NONE
CHAR05_02 contains: CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE
CHAR05_02 contains: LOCKED 유지
Source Registry marker: REGISTRY_STATE: FILLED_BY_CHAR00_01
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR05_04 and later tasks: LOCKED
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
12. `CharacterDesign/MCP/TASKS/CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT.md`
13. `CharacterDesign/MCP/REPORTS/CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT_RESULT.md`
14. `CharacterDesign/MCP/REPORTS/CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE_RESULT.md`
15. `CharacterDesign/MCP/REPORTS/CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK_RESULT.md`
16. `CharacterDesign/01_FIXED_SPEC/01_CHARACTER_GAMEPLAY_RULES.md`
17. `CharacterDesign/01_FIXED_SPEC/02_CHARACTER_INPUT_RULES.md`
18. `CharacterDesign/01_FIXED_SPEC/05_CHARACTER_COMBAT_RULES.md`
19. `CharacterDesign/01_FIXED_SPEC/06_CHARACTER_MAP_INTEGRATION_RULES.md`
20. `CharacterDesign/01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
21. `CharacterDesign/03_DATA_SCHEMA/CHARACTER_DAMAGE_SCHEMA.md`
22. `CharacterDesign/03_DATA_SCHEMA/CHARACTER_ACTION_SCHEMA.md`
23. `CharacterDesign/04_TEST_FIXTURES/COMBAT_COURSE_SPEC.md`
24. Current character runtime under `Assets/_Game/Character/Runtime/`
25. Current character EditMode tests under `Assets/_Game/Tests/EditMode/Character/`
26. Legacy health/hazard/death examples for read-only reference only:
    - `Assets/_Legacy/StarNight/Scripts/Runtime/Player/**`
    - `Assets/_Legacy/StarNight/Scripts/Runtime/Objects/**`
    - `Assets/_Legacy/_Game/Player/Tests/**`
    - `Assets/_Legacy/_Game/Interaction/**`

Do not read or start any `CHAR05_04`, `CHAR05_05`, or `CHAR06` task body.

## Allowed Writes

Allowed runtime writes:

```text
Assets/_Game/Character/Runtime/Survival/**
```

Allowed test writes:

```text
Assets/_Game/Tests/EditMode/Character/Survival/**
```

Conditional bridge writes:

```text
Assets/_Game/Character/Runtime/Combat/**
Assets/_Game/Character/Runtime/Equipment/**
Assets/_Game/Tests/EditMode/Character/Combat/**
Assets/_Game/Tests/EditMode/Character/Equipment/**
```

Use conditional bridge writes only if a tiny adapter is required to convert existing CHAR04/CHAR05 damage candidate types into the unified survival damage request. Do not rewrite combat, bomb, rope, carry, throw, or impact behavior.

Required report:

```text
CharacterDesign/MCP/REPORTS/CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE_RESULT.md
```

Forbidden:

- Runtime or test changes outside allowed Survival paths and conditional bridge paths.
- asmdef changes unless compile proves the new files are not included by existing `Game.Character.Runtime` or `Game.Character.Tests.EditMode`; if asmdef change is unavoidable, BLOCK and report instead.
- Scene, prefab, physics layer asset, inputactions, Packages, ProjectSettings, MapDesign, MAP runtime, Tilemap, camera, animation, audio, presentation, save data, or legacy code changes.
- HUD, inventory UI, run-state UI, checkpoint scene loading, scene reload, GameObject activation/deactivation, or player transform teleport.
- Bomb or rope behavior changes.
- Adding a basic attack, melee, shoot, dash, wall jump, or double jump.
- Adding ActionId values beyond the existing locked set.
- Animator-event-owned damage/death/run failure authority.
- Unity physics callback-owned hazard or death authority.
- Opening or installing any future task.
- Editing `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md` or `CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md` during task execution.

## Required Implementation

### 1. Health State and Damage Application

Implement pure health values and policy.

Required behavior:

```text
health state records actor id, target kind, current health, max health, and invulnerability remaining seconds
current and max health are validated and clamped
positive damage request reduces current health down to zero
zero or negative damage amount creates no health change
invulnerability active suppresses damage unless the request explicitly bypasses invulnerability
damage application returns result values and does not mutate the input state
```

### 2. Unified Damage Request

Implement a unified survival damage request that can consume existing candidates.

Required behavior:

```text
contact damage candidate can be represented as survival damage request
impact damage candidate can be represented as survival damage request
explosion damage candidate can be represented as survival damage request
hazard damage candidate can be represented as survival damage request
request records source kind, target id, target kind, amount, direction, and bypass-invulnerability flag
request does not apply HUD, score, knockback, stun, removal, death, or presentation directly
```

If existing candidate types can be consumed without editing their files, prefer Survival-side adapter methods.

### 3. Hazard Candidates

Implement hazard candidate contracts.

Required behavior:

```text
spike, crush, fire, and generic damage hazards can produce damage requests
void or out-of-bounds hazard can produce fatal run failure request
hazard request records source kind and map/world position or cell when available
hazard request does not query live physics or mutate MAP/Tilemap
```

### 4. Death Request

Implement death result values.

Required behavior:

```text
lethal damage to any actor creates death request for that actor
non-lethal damage creates no death request
enemy or non-player death does not create player run failure
death request records actor id, target kind, cause, and source id where available
death request does not destroy GameObjects or play animation
```

### 5. Run Failure and Return Request

Implement player run failure as request/data only.

Required behavior:

```text
player death creates run failure request
fatal void/out-of-bounds hazard creates run failure request
run failure request records reason, actor id, and optional return destination token
return/retry destination is data only
run failure request does not reload scenes, mutate save data, open UI, or move the player transform
```

CHAR05_04 owns run-state HUD and presentation bridge.

### 6. Authority and Forbidden Feature Guard

Keep decision authority pure.

Required behavior:

```text
no Animator event authority
no Unity physics callback authority
no direct MAP or Tilemap mutation
no scene reload, save mutation, HUD, audio, or presentation
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
Health_DamageReducesHealthAndClampsAtZero
Health_NonPositiveDamageCreatesNoChange
Health_InvulnerabilitySuppressesDamageUnlessBypassed
Health_LethalDamageCreatesDeathRequest
Health_NonLethalDamageCreatesNoDeathOrRunFailure
Damage_ContactImpactExplosionAndHazardCanBecomeUnifiedRequests
Hazard_SpikeCrushFireCreateDamageCandidates
Hazard_VoidOrOutOfBoundsCreatesRunFailureRequest
Death_EnemyDeathDoesNotCreatePlayerRunFailure
RunFailure_PlayerDeathCreatesRunFailureRequest
RunFailure_ReturnDestinationIsDataOnlyAndDoesNotReloadSceneOrSave
SurvivalRuntime_DoesNotUseAnimatorPhysicsSceneHudSaveOrForbiddenActions
```

Names may vary if they fit existing conventions, but the report must map actual test names to these twelve required behaviors.

Run:

```text
Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Expected minimum tests: 146
Expected result: PASS
```

The expected minimum is previous 134 plus at least 12 CHAR05_03 tests.

PlayMode is not required for this task.

## Required Report

Write:

```text
CharacterDesign/MCP/REPORTS/CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE_RESULT.md
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
HEALTH_STATE_AND_DAMAGE
UNIFIED_DAMAGE_REQUESTS
HAZARD_CANDIDATES
DEATH_REQUEST
RUN_FAILURE_AND_RETURN_REQUEST
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

- [ ] CHAR05_02 PASS/hash verified.
- [ ] Source registry marker/hash verified.
- [ ] Positive damage reduces health and clamps at zero.
- [ ] Non-positive damage creates no health change.
- [ ] Invulnerability suppresses damage unless bypassed.
- [ ] Contact, impact, explosion, and hazard candidates can become unified survival damage requests.
- [ ] Spike/crush/fire hazards create damage candidates.
- [ ] Void/out-of-bounds hazard creates run failure request.
- [ ] Lethal damage creates death request.
- [ ] Non-lethal damage creates no death or run failure.
- [ ] Enemy/non-player death does not create player run failure.
- [ ] Player death creates run failure request.
- [ ] Return destination is data only.
- [ ] No scene reload, save mutation, HUD, audio, animation, or presentation side effect exists.
- [ ] Animator events and physics callbacks are not authority.
- [ ] Forbidden basic attack/movement features remain absent.
- [ ] ActionId locked set remains unchanged.
- [ ] Character EditMode tests pass with at least 146 tests.
- [ ] Unity compile errors 0.
- [ ] Scope validation completed.
- [ ] CHAR05_04 remains locked.

## Completion Rule

If STATUS is PASS:

- Finalize CHAR05_03 to COMPLETE.
- Set Current Task after finalize to NONE.
- Keep `CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE` locked.
- Do not auto-open CHAR05_04.

If STATUS is FAIL or BLOCKED:

- Keep CHAR05_03 CURRENT.
- Do not open CHAR05_04.

The NEXT section must include:

```text
Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
```

only when PASS/finalized. If not PASS, state why the task remains CURRENT.
