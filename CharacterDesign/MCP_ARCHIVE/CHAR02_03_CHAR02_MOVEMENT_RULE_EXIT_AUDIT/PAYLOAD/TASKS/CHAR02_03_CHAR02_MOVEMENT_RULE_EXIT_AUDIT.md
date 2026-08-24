# TASK: CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT

```yaml
task_id: CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT
phase: CHAR02_MOVEMENT_RULE_VALIDATION
task_type: AUDIT
created: 2026-08-23
workflow: MCP_INBOX_PATCH_ONLY
write_scope: REPORT_ONLY
```

## Objective

Audit the completed CHAR02 movement grammar validation work and decide whether CHAR02 can exit.

This task does not implement fixes. If the audit finds that the locked movement grammar is not actually satisfied, report it as a CHAR02 exit blocker and keep CHAR03 locked for a later corrective patch.

## Entry Gate

Before doing any audit work, verify all of the following:

```text
Current Task: TASKS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT.md
CHAR02_01 Result: PASS
CHAR02_02 Result: PASS
CHAR02_02 Result SHA-256: 09e033b8d559afbefa7f761f4367c5294e06ab9f44cdd0f153966bf0af5cb192
Source Registry marker: REGISTRY_STATE: FILLED_BY_CHAR00_01
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
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
9. `CharacterDesign/MCP/TASKS/CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES.md`
10. `CharacterDesign/MCP/REPORTS/CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES_RESULT.md`
11. `CharacterDesign/MCP/TASKS/CHAR02_02_VALIDATE_THREE_CELL_FAILURE_AND_FORBIDDEN_MOVEMENT.md`
12. `CharacterDesign/MCP/REPORTS/CHAR02_02_VALIDATE_THREE_CELL_FAILURE_AND_FORBIDDEN_MOVEMENT_RESULT.md`
13. `CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`
14. `CharacterDesign/01_FIXED_SPEC/01_CHARACTER_GAMEPLAY_RULES.md`
15. `CharacterDesign/01_FIXED_SPEC/02_CHARACTER_INPUT_RULES.md`
16. `CharacterDesign/01_FIXED_SPEC/03_CHARACTER_MOVEMENT_RULES.md`
17. `CharacterDesign/01_FIXED_SPEC/06_CHARACTER_MAP_INTEGRATION_RULES.md`
18. `CharacterDesign/01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
19. `CharacterDesign/04_TEST_FIXTURES/MOVEMENT_COURSE_SPEC.md`
20. Current character runtime and EditMode test code touched by CHAR01/CHAR02.

Do not read or start any future task body beyond this task.

## Allowed Writes

Allowed:

```text
CharacterDesign/MCP/REPORTS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT_RESULT.md
```

Forbidden:

- Runtime source changes.
- Test source changes.
- Scene, prefab, inputactions, asmdef, Packages, ProjectSettings, or MapDesign changes.
- Status or master list edits by the task body.
- Starting `CHAR03_01` or installing any `CHAR03_*` task file.
- Implementing fixes for findings discovered during the audit.

## Audit Gates

### 1. Prior Evidence and State

Verify:

- CHAR02_01 is COMPLETE and its report is PASS.
- CHAR02_02 is COMPLETE and its report is PASS.
- Current Task is exactly this task.
- Source registry marker/hash match the entry gate.
- CHAR03_01 and later tasks remain locked.

### 2. Movement Grammar Coverage

Verify the completed tests and implementation evidence cover the locked grammar:

```text
Logical cell: 1 world unit
Player collider baseline: Capsule 0.72 × 0.90
2-cell height: reachable
Same-level 2-cell gap: pass
Same-level 3-cell gap: fail for valid basic movement grammar
Forbidden movement: wall jump / dash / double jump absent
Basic attack/melee/shoot: absent
```

The 3-cell rule is not satisfied by filename or nominal test count alone. The audit must explicitly evaluate the CHAR02_02 finding that delayed coyote jumps may clear a 3-cell same-level gap.

CHAR02 EXIT may be approved only if one of these is true:

1. The audit proves delayed coyote jump is outside the locked 3-cell gap grammar; or
2. The audit proves legal delayed/coyote jump inputs still cannot clear the 3-cell same-level gap; or
3. The audit identifies a corrective patch already applied before this audit and verifies it without scope violations.

If none is true, CHAR02 EXIT must be rejected. Do not tune values or add tests inside this audit task.

### 3. Course Fixture and Determinism

Verify:

- Movement courses use 1 unit logical cells.
- Course fixtures use the locked capsule baseline, or explicitly document why the fixture simulator representation is equivalent.
- 2-cell and 3-cell gap courses differ only in intended gap width and not by hidden trajectory assistance.
- Test inputs are deterministic and fixed-step.
- Results are reproducible across repeated runs.
- There is no hardcoded pass/fail result that bypasses motion simulation.

### 4. Regression Tests

Run the relevant Unity tests:

```text
Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Expected minimum tests: 52
Expected result: PASS if audit evidence is otherwise valid
PlayMode: NOT RUN unless needed for diagnosis
```

If tests pass but the movement grammar is semantically invalid, the report status must still be FAIL or BLOCKED according to the evidence.

### 5. Scope and Dependency

Verify no out-of-scope change was introduced by CHAR02:

- Runtime changes are limited to CHAR01 movement implementation and CHAR02 permitted support files.
- CHAR02 did not modify MAP, Tilemap, Scene, Prefab, inputactions, asmdef, Packages, or ProjectSettings.
- CHAR02 did not start room transitions, map query integration, items, enemies, bombs, ropes, health, HUD, or generated-map integration.
- Known stale Map PlayMode asmdef reference remains out-of-scope unless it blocks the required Character EditMode validation.

### 6. Dependency Ledger for CHAR03

Record whether these dependencies are ready or deferred:

```text
MAP world query / coordinate conversion
Room boundary detection and readiness gate
Camera room transition policy
Terrain mutation request API
Generated map route integration
```

CHAR03_01 can become eligible only after CHAR02 exit is approved. If CHAR02 exit is rejected, CHAR03_01 remains blocked even if the MAP dependency itself is separable.

## Required Report

Write:

```text
CharacterDesign/MCP/REPORTS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT_RESULT.md
```

The report must include these sections:

```text
TASK
STATUS
SUMMARY
READ
CHANGED
CREATED
TEST
UNITY
MOVEMENT_GRAMMAR_COVERAGE
COURSE_FIXTURE_DETERMINISM
FORBIDDEN_FEATURE_SCAN
DEPENDENCY_LEDGER
OUT_OF_SCOPE_FINDINGS
CHAR02 EXIT
CHAR03_01 ENTRY
DONE CONDITIONS
NEXT
```

If the audit passes all gates, include exactly:

```text
CHAR02 EXIT: APPROVED
CHAR03_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH
```

If any gate fails, include exactly:

```text
CHAR02 EXIT: REJECTED
CHAR03_01 ENTRY: BLOCKED
```

## Done Conditions

All done conditions must be checked in the report:

- [ ] CHAR02_01 PASS report verified.
- [ ] CHAR02_02 PASS report verified.
- [ ] Current status chain verified.
- [ ] Source registry marker/hash verified.
- [ ] Movement grammar coverage audited.
- [ ] Coyote delayed jump 3-cell risk explicitly accepted or rejected with evidence.
- [ ] Course fixture determinism audited.
- [ ] Forbidden movement absence audited.
- [ ] Character EditMode tests run and result recorded.
- [ ] Scope validation completed.
- [ ] Dependency ledger completed.
- [ ] CHAR02 EXIT decision recorded with exact required text.
- [ ] CHAR03_01 ENTRY decision recorded with exact required text.
- [ ] No code/test/status/master modifications made by this task.

## Completion Rule

If STATUS is PASS:

- Finalize CHAR02_03 to COMPLETE.
- Set Current Task after finalize to NONE.
- Keep `CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE` locked.
- Do not auto-open CHAR03_01.

If STATUS is FAIL or BLOCKED:

- Keep CHAR02_03 CURRENT.
- Do not open CHAR03_01.

The NEXT section must include:

```text
Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
```
only when PASS/finalized. If not PASS, state why the task remains CURRENT.
