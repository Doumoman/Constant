# TASK: CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT

```yaml
task_id: CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT
phase: CHAR05_EQUIPMENT_SURVIVAL_AND_RUN
task_type: EXIT_AUDIT
created: 2026-08-24
workflow: MCP_INBOX_PATCH_ONLY
write_scope: REPORT_ONLY_AUDIT
```

## Objective

Audit the full CHAR05 equipment, survival, run-state, HUD snapshot, and presentation bridge phase, then approve or reject CHAR05 exit.

This task owns verification only:

```text
CHAR05_01 bomb/explosion/terrain mutation request contract is complete and request-only
CHAR05_02 rope placement/segment/climb contract is complete and request-only
CHAR05_03 health/hazard/death/run failure contract is complete and request-only
CHAR05_04 run state/HUD snapshot/presentation event bridge is complete and request-only
contracts compose without hidden ownership conflicts
deferred items are explicitly assigned to CHAR06 or later real integration layers
no real UI, scene reload, save mutation, audio, animation, prefab, GameObject, Tilemap, or MAP mutation exists
no forbidden movement or separate basic attack exists
Character EditMode tests remain PASS with at least 158 tests
```

This task must not implement new runtime code, tests, scene objects, prefab wiring, physics layers, HUD, audio, animation, save data, MAP mutation, or future task behavior.

## Entry Gate

Before changing anything, verify:

```text
Current Task: TASKS/CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT.md
CHAR05_04 Result: PASS
CHAR05_04 Result SHA-256: 321877eda8f80333bb285abd9d850cd7d9a44577ac85dfc53515d7a47331572c
CHAR05_04 contains: Current Task after finalize: NONE
CHAR05_04 contains: CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT
CHAR05_04 contains: LOCKED 유지
Source Registry marker: REGISTRY_STATE: FILLED_BY_CHAR00_01
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR06 and later tasks: LOCKED
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
14. `CharacterDesign/MCP/TASKS/CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE.md`
15. `CharacterDesign/MCP/REPORTS/CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE_RESULT.md`
16. `CharacterDesign/MCP/TASKS/CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE.md`
17. `CharacterDesign/MCP/REPORTS/CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE_RESULT.md`
18. Current task body
19. `CharacterDesign/01_FIXED_SPEC/01_CHARACTER_GAMEPLAY_RULES.md`
20. `CharacterDesign/01_FIXED_SPEC/02_CHARACTER_INPUT_RULES.md`
21. `CharacterDesign/01_FIXED_SPEC/05_CHARACTER_COMBAT_RULES.md`
22. `CharacterDesign/01_FIXED_SPEC/06_CHARACTER_MAP_INTEGRATION_RULES.md`
23. `CharacterDesign/01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
24. `CharacterDesign/03_DATA_SCHEMA/CHARACTER_INVENTORY_SCHEMA.md`
25. `CharacterDesign/03_DATA_SCHEMA/CHARACTER_DAMAGE_SCHEMA.md`
26. `CharacterDesign/03_DATA_SCHEMA/CHARACTER_ACTION_SCHEMA.md`
27. Current character runtime under `Assets/_Game/Character/Runtime/`
28. Current character EditMode tests under `Assets/_Game/Tests/EditMode/Character/`
29. Relevant compile/test output and `git status`

Do not read or start any `CHAR06` task body.

## Allowed Writes

Only this report may be written:

```text
CharacterDesign/MCP/REPORTS/CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT_RESULT.md
```

Forbidden writes:

- Runtime code.
- Test code.
- asmdef files.
- Scenes, prefabs, physics layers, inputactions, Packages, ProjectSettings, MapDesign, MAP runtime, Tilemap, camera, animation, audio, UI, save data, or legacy code.
- `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md` during task execution.
- `CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md` during task execution.
- Future task bodies.

If a defect needs code changes, write STATUS: FAIL with the defect and do not patch it in this task.

## Required Audit

### 1. Phase Result Ledger

Verify:

```text
CHAR05_01 STATUS PASS, report hash, and done conditions
CHAR05_02 STATUS PASS, report hash, and done conditions
CHAR05_03 STATUS PASS, report hash, and done conditions
CHAR05_04 STATUS PASS, report hash, and done conditions
all four tasks finalized Current Task after finalize: NONE
CHAR05_05 was opened only by this MCP_INBOX package
```

### 2. Bomb and Terrain Request Audit

Verify:

```text
bomb placement and spend are requests only
fuse creates explosion request exactly once
terrain mutation request targets destructible cells only
terrain request does not mutate MAP, Tilemap, tile assets, scene, prefab, or physics assets
explosion damage candidates are requests only
```

### 3. Rope Traversal Audit

Verify:

```text
rope placement and spend are requests only
segment generation is vertical, deterministic, bounded, and blocker-aware
climb motor request is vertical and clamped to rope bounds
rope traversal does not grant wall jump, dash, double jump, or extra air control
rope contract does not instantiate prefabs, mutate scene, mutate MAP, or wire live physics
```

### 4. Survival and Run Failure Audit

Verify:

```text
health state is immutable and damage application returns new state
unified damage requests consume contact, impact, explosion, and hazard candidates
invulnerability suppresses damage unless bypassed
lethal damage creates death request
player death or void hazard creates run failure request
enemy/non-player death does not create player run failure
run failure/return destination remains data only
no HUD, scene reload, save mutation, audio, animation, GameObject, or transform side effect exists
```

### 5. Run State and Presentation Bridge Audit

Verify:

```text
run inventory starts with centralized bomb=4 and rope=4
bomb/rope spend requests update run inventory as data
HUD snapshot exposes health, inventory, status, and return token as data only
presentation event requests cover damage, death, run failure, bomb, rope, and inventory changes
event ordering is deterministic and deduplicated
presentation bridge does not play UI, audio, animation, particles, camera, scene, or save side effects
```

### 6. Forbidden Feature and Dependency Audit

Verify:

```text
no basic attack
no melee
no shoot
no dash
no wall jump
no double jump
ActionId locked set unchanged
Animator events are not authority
Unity physics callbacks are not authority
Unity UI/audio/scene/save systems are not authority
MAP dependency remains public coordinate/query/mutation contract only
```

### 7. Test and Scope Audit

Run:

```text
Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Expected minimum tests: 158
Expected result: PASS
```

PlayMode is not required for this exit audit unless compile/test evidence indicates a CHAR05-specific runtime integration risk.

Confirm scope:

```text
runtime/test changes during audit: 0
scene/prefab/project/package/MAP/UI/audio/save changes during audit: 0
report-only task write: 1
```

## Required Report

Write:

```text
CharacterDesign/MCP/REPORTS/CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT_RESULT.md
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
CHAR05_PHASE_LEDGER
BOMB_TERRAIN_REQUEST_AUDIT
ROPE_TRAVERSAL_AUDIT
SURVIVAL_RUN_FAILURE_AUDIT
RUN_STATE_PRESENTATION_AUDIT
FORBIDDEN_FEATURE_GUARD
DEPENDENCY_DIRECTION
SCOPE_VALIDATION
DEFERRED_LEDGER
CHAR05_EXIT_DECISION
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
```

## Done Conditions

All done conditions must be checked in the report:

- [ ] CHAR05_01 PASS/hash/done conditions verified.
- [ ] CHAR05_02 PASS/hash/done conditions verified.
- [ ] CHAR05_03 PASS/hash/done conditions verified.
- [ ] CHAR05_04 PASS/hash/done conditions verified.
- [ ] Source registry marker/hash verified.
- [ ] Bomb/fuse/explosion/terrain mutation request contract remains intact.
- [ ] Rope placement/segment/climb contract remains intact.
- [ ] Health/hazard/death/run failure contract remains intact.
- [ ] Run state/HUD snapshot/presentation bridge contract remains intact.
- [ ] All damage, terrain, HUD, presentation, save, scene, audio, and UI outputs remain request/data only.
- [ ] No basic attack or forbidden movement feature exists.
- [ ] ActionId locked set remains unchanged.
- [ ] Animator events are not authority.
- [ ] Unity physics callbacks are not authority.
- [ ] UI/audio/scene/save systems are not authority.
- [ ] Character EditMode tests pass with at least 158 tests.
- [ ] Unity compile errors 0.
- [ ] Audit wrote no runtime/test/project files.
- [ ] CHAR05_EXIT_DECISION is APPROVED or REJECTED with evidence.
- [ ] CHAR06_01 remains locked.

## Completion Rule

If STATUS is PASS and `CHAR05_EXIT_DECISION: APPROVED`:

- Finalize CHAR05_05 to COMPLETE.
- Set Current Task after finalize to NONE.
- Keep `CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES` locked.
- Do not auto-open CHAR06_01.

If STATUS is FAIL or BLOCKED:

- Keep CHAR05_05 CURRENT.
- Do not open CHAR06_01.

The NEXT section must include:

```text
Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
```

only when PASS/finalized. If not PASS, state why the task remains CURRENT.
