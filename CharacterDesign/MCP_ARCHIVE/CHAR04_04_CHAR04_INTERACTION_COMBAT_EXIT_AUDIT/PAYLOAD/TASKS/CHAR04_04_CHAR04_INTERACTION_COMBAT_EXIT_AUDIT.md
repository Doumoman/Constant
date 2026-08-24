# TASK: CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT

```yaml
task_id: CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT
phase: CHAR04_INTERACTION_AND_COMBAT
task_type: EXIT_AUDIT
created: 2026-08-24
workflow: MCP_INBOX_PATCH_ONLY
write_scope: REPORT_ONLY_AUDIT
```

## Objective

Audit the full CHAR04 interaction and combat phase, then approve or reject CHAR04 exit.

This task owns verification only:

```text
CHAR04_01 carry/drop/throw contract is complete and still isolated
CHAR04_02 stomp/stun/removal/contact damage contract is complete and still isolated
CHAR04_03 thrown/world impact contract and no-basic-attack guard are complete
contracts compose without hidden ownership conflicts
deferred items are explicitly assigned to CHAR05/CHAR06 or integration layers
no forbidden movement or separate basic attack exists
Character EditMode tests remain PASS with at least 110 tests
```

This task must not implement new runtime code, tests, scene objects, prefab wiring, physics layers, health, bomb, rope, terrain mutation, HUD, presentation, or future task behavior.

## Entry Gate

Before changing anything, verify:

```text
Current Task: TASKS/CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT.md
CHAR04_03 Result: PASS
CHAR04_03 Result SHA-256: 14752158017446a9f49ff4c7088fdeb043b6886b9bfa48d60f378fe5ba85c1ab
CHAR04_03 contains: Current Task after finalize: NONE
CHAR04_03 contains: CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT
CHAR04_03 contains: LOCKED 유지
Source Registry marker: REGISTRY_STATE: FILLED_BY_CHAR00_01
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR05 and later tasks: LOCKED
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
14. `CharacterDesign/MCP/TASKS/CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK.md`
15. `CharacterDesign/MCP/REPORTS/CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK_RESULT.md`
16. Current task body
17. `CharacterDesign/01_FIXED_SPEC/04_CHARACTER_INTERACTION_RULES.md`
18. `CharacterDesign/01_FIXED_SPEC/05_CHARACTER_COMBAT_RULES.md`
19. `CharacterDesign/01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
20. `CharacterDesign/04_TEST_FIXTURES/INTERACTION_COURSE_SPEC.md`
21. `CharacterDesign/04_TEST_FIXTURES/COMBAT_COURSE_SPEC.md`
22. Current character runtime under `Assets/_Game/Character/Runtime/`
23. Current character EditMode tests under `Assets/_Game/Tests/EditMode/Character/`
24. Relevant compile/test output and `git status`

Do not read or start any `CHAR05` or `CHAR06` task body.

## Allowed Writes

Only this report may be written:

```text
CharacterDesign/MCP/REPORTS/CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT_RESULT.md
```

Forbidden writes:

- Runtime code.
- Test code.
- asmdef files.
- Scenes, prefabs, physics layers, inputactions, Packages, ProjectSettings, MapDesign, MAP runtime, Tilemap, camera, animation, presentation, or legacy code.
- `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md` during task execution.
- `CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md` during task execution.
- Future task bodies.

If a defect needs code changes, write STATUS: FAIL with the defect and do not patch it in this task.

## Required Audit

### 1. Phase Result Ledger

Verify:

```text
CHAR04_01 STATUS PASS, report hash, and done conditions
CHAR04_02 STATUS PASS, report hash, and done conditions
CHAR04_03 STATUS PASS, report hash, and done conditions
all three tasks finalized Current Task after finalize: NONE
CHAR04_04 was opened only by this MCP_INBOX package
```

### 2. Interaction Contract Audit

Verify:

```text
single carry slot remains single-owner and deterministic
safe drop still refuses blocked destination
directional throw still preserves Up priority and owner collision grace
carry/drop/throw behavior was not rewritten by later tasks
stunned small enemy carry bridge remains compatible with CHAR04_01 contract
```

### 3. Combat Contract Audit

Verify:

```text
descending top contact is the only valid stomp
first stomp on normal small enemy produces stun result
second stomp on stunned small enemy produces removal result
side/bottom hostile contact creates damage candidate only
damage candidates do not apply health/life/HP/death
player rebound is separate from enemy result
```

### 4. Impact Contract Audit

Verify:

```text
moving thrown object can create enemy impact damage candidate only
solid world impact creates object stop/rest request only
owner collision grace suppresses owner/self impact
stationary or below-threshold impact source creates no event
impact result separates object, enemy, and player request slots
impact contract does not mutate terrain, health, HP, death, score, or presentation
```

### 5. Forbidden Feature and Dependency Audit

Verify:

```text
no basic attack
no melee
no shoot
no dash
no wall jump
no double jump
ActionId locked set unchanged
Animator events are not interaction/combat authority
Unity physics callbacks are not decision authority
MAP dependency remains public coordinate/query/mutation contract only
```

### 6. Test and Scope Audit

Run:

```text
Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Expected minimum tests: 110
Expected result: PASS
```

PlayMode is not required for this exit audit unless compile/test evidence indicates a CHAR04-specific runtime integration risk.

Confirm scope:

```text
runtime/test changes during audit: 0
scene/prefab/project/package/MAP changes during audit: 0
report-only task write: 1
```

## Required Report

Write:

```text
CharacterDesign/MCP/REPORTS/CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT_RESULT.md
```

The report must include:

```text
TASK
STATUS
SUMMARY
READ
EVIDENCE_HASHES
CHANGED
CREATED
TEST
UNITY
CHAR04_PHASE_LEDGER
INTERACTION_CONTRACT_AUDIT
COMBAT_CONTRACT_AUDIT
IMPACT_CONTRACT_AUDIT
FORBIDDEN_FEATURE_GUARD
DEPENDENCY_DIRECTION
SCOPE_VALIDATION
DEFERRED_LEDGER
CHAR04_EXIT_DECISION
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
```

## Done Conditions

All done conditions must be checked in the report:

- [ ] CHAR04_01 PASS/hash/done conditions verified.
- [ ] CHAR04_02 PASS/hash/done conditions verified.
- [ ] CHAR04_03 PASS/hash/done conditions verified.
- [ ] Source registry marker/hash verified.
- [ ] Carry/drop/throw contract remains intact.
- [ ] Stunned small enemy carry bridge remains compatible.
- [ ] Stomp/stun/removal/contact damage contract remains intact.
- [ ] Thrown/world impact contract remains intact.
- [ ] Damage and impact candidates do not apply health, HP, death, score, or presentation.
- [ ] No basic attack or forbidden movement feature exists.
- [ ] ActionId locked set remains unchanged.
- [ ] Animator events are not authority.
- [ ] Unity physics callbacks are not decision authority.
- [ ] Character EditMode tests pass with at least 110 tests.
- [ ] Unity compile errors 0.
- [ ] Audit wrote no runtime/test/project files.
- [ ] CHAR04_EXIT_DECISION is APPROVED or REJECTED with evidence.
- [ ] CHAR05_01 remains locked.

## Completion Rule

If STATUS is PASS and `CHAR04_EXIT_DECISION: APPROVED`:

- Finalize CHAR04_04 to COMPLETE.
- Set Current Task after finalize to NONE.
- Keep `CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST` locked.
- Do not auto-open CHAR05_01.

If STATUS is FAIL or BLOCKED:

- Keep CHAR04_04 CURRENT.
- Do not open CHAR05_01.

The NEXT section must include:

```text
Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
```

only when PASS/finalized. If not PASS, state why the task remains CURRENT.
