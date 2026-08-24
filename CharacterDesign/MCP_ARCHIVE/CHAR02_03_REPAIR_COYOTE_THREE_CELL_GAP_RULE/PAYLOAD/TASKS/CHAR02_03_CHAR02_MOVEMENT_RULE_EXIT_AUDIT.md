# TASK: CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT

```yaml
task_id: CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT
phase: CHAR02_MOVEMENT_RULE_VALIDATION
task_type: CHANGE_CONTROL_REPAIR_AND_EXIT_AUDIT
revision: repair_coyote_three_cell_gap_rule
created: 2026-08-23
workflow: MCP_INBOX_PATCH_ONLY
write_scope: LIMITED_CHARACTER_MOVEMENT_REPAIR_PLUS_REPORT
```

## Objective

Repair the CHAR02 movement grammar blocker found by the first CHAR02_03 audit, then rerun the CHAR02 exit audit.

The blocker is specific:

```text
Legal coyote delayed jump can clear the same-level 3-cell gap.
Evidence: CHAR02_03 failed report, x=3.171 or higher with Move+Jump only.
```

This task must make the locked movement grammar true:

```text
2-cell height: reachable
same-level 2-cell gap: pass
same-level 3-cell gap: fail for all legal basic movement combinations, including coyote/buffer timing
forbidden movement: wall jump / dash / double jump absent
basic attack/melee/shoot: absent
```

Do not open CHAR03. CHAR03_01 remains blocked until this task produces a PASS report with `CHAR02 EXIT: APPROVED`.

## Entry Gate

Before changing anything, verify:

```text
Current Task: TASKS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT.md
CHAR02_01 Result: PASS
CHAR02_02 Result: PASS
Previous CHAR02_03 Result: FAIL
Previous CHAR02_03 Result SHA-256: e5fac10bce6791006c2549134834b8d518d0f9aa1d29d276595ce87203208043
Previous CHAR02_03 contains: CHAR02 EXIT: REJECTED
Previous CHAR02_03 contains: CHAR03_01 ENTRY: BLOCKED
Source Registry marker: REGISTRY_STATE: FILLED_BY_CHAR00_01
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR03_01 and later tasks: LOCKED
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
13. `CharacterDesign/MCP/TASKS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT.md`
14. `CharacterDesign/MCP/REPORTS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT_RESULT.md`
15. `CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`
16. `CharacterDesign/01_FIXED_SPEC/01_CHARACTER_GAMEPLAY_RULES.md`
17. `CharacterDesign/01_FIXED_SPEC/02_CHARACTER_INPUT_RULES.md`
18. `CharacterDesign/01_FIXED_SPEC/03_CHARACTER_MOVEMENT_RULES.md`
19. `CharacterDesign/01_FIXED_SPEC/06_CHARACTER_MAP_INTEGRATION_RULES.md`
20. `CharacterDesign/01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
21. `CharacterDesign/04_TEST_FIXTURES/MOVEMENT_COURSE_SPEC.md`
22. Current character movement runtime code.
23. Current character EditMode movement and movement-course tests.

Do not read or start any `CHAR03_*` task body.

## Allowed Writes

Runtime repair is allowed only under:

```text
Assets/_Game/Character/Runtime/Movement/
```

Test repair is allowed only under:

```text
Assets/_Game/Tests/EditMode/Character/
```

Required report:

```text
CharacterDesign/MCP/REPORTS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT_RESULT.md
```

Allowed runtime changes are limited to movement tuning or coyote/jump semantics needed to make the locked 3-cell gap rule true.

Preferred runtime files to inspect before changing:

```text
Assets/_Game/Character/Runtime/Movement/CharacterGroundMotorSettings.cs
Assets/_Game/Character/Runtime/Movement/CharacterAirControlSettings.cs
Assets/_Game/Character/Runtime/Movement/CharacterJumpSettings.cs
Assets/_Game/Character/Runtime/Movement/CharacterJumpController.cs
Assets/_Game/Character/Runtime/Movement/CharacterJumpState.cs
Assets/_Game/Character/Runtime/Movement/CharacterGravitySettings.cs
```

Allowed test additions or updates are limited to:

- Locking the coyote delayed 3-cell failure case.
- Updating expectation values caused by deliberate movement tuning changes.
- Preserving existing 2-cell height, 2-cell gap, forbidden movement, and single-jump-consumption coverage.

Forbidden:

- Adding wall jump, dash, double jump, basic attack, melee, or shoot.
- Removing coyote time entirely unless the report proves this is still consistent with locked input/movement rules.
- Weakening the 2-cell height or 2-cell gap requirements.
- Marking tests Ignore/Explicit or hiding failures through conditional compilation.
- Hardcoding movement-course pass/fail results.
- Scene, prefab, inputactions, asmdef, Packages, ProjectSettings, Tilemap, MapDesign, MAP runtime, generated map, item, enemy, bomb, rope, health, HUD, or presentation changes.
- Editing `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md` or `CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md` during task execution.
- Opening or installing any `CHAR03_*` task.

## Required Repair Strategy

Use evidence, not a guessed value.

1. Reproduce the failing legal coyote delayed jump case from the failed audit.
2. Identify the smallest coherent correction that satisfies all movement grammar gates.
3. Add deterministic regression coverage for the exploit.
4. Run the full Character EditMode test assembly.
5. Rerun the CHAR02 exit audit gates and write the report.

Acceptable correction categories:

```text
A. Tuning correction:
   Adjust runSpeed / air max speed / jump velocity / gravity / coyote window only as needed.
   Must preserve 2-cell height and same-level 2-cell gap.

B. Coyote semantics correction:
   Keep coyote time, but prevent coyote from extending the effective horizontal takeoff point enough to clear 3 cells.
   Must preserve legitimate ledge forgiveness and single-jump behavior.

C. Combined correction:
   Small tuning plus small coyote semantics change, if a single change cannot satisfy all gates cleanly.
```

Do not satisfy the audit by redefining coyote delayed jump as out-of-scope. The fixed fixture contract includes coyote/buffer usage in basic movement combinations.

## Required New or Updated Test Coverage

Add or update tests so the following are verified by deterministic EditMode tests:

```text
ThreeCellGapCourse_CoyoteDelayedJumpDoesNotClearSameLevelThreeCellGap
ThreeCellGapCourse_CoyoteDelaySweepNeverClearsSameLevelThreeCellGap
TwoCellGapCourse_StillPassesAfterCoyoteRepair
TwoCellHeightCourse_StillReachesTwoCellsAfterCoyoteRepair
ForbiddenMovement_StillHasNoWallJumpDashDoubleJumpOrBasicAttack
```

Names may vary if they fit the existing test naming convention, but the report must map actual test names to these five required behaviors.

The coyote delay sweep must cover at least these legal delay samples when they are within the configured coyote window:

```text
0.000s
0.0167s
0.0333s
0.0500s
0.0667s
0.0833s
0.1000s
```

If the corrected coyote window is shorter than any sample, the test must explicitly record that samples beyond the window do not start a jump and therefore cannot clear the gap.

## Required Audit Gates

### 1. Prior Evidence and State

Verify:

- CHAR02_01 PASS.
- CHAR02_02 PASS.
- Previous CHAR02_03 FAIL and rejection evidence verified.
- Current Task is still this task.
- Source registry marker/hash match.
- CHAR03_01 and later tasks remain locked.

### 2. Movement Grammar Coverage

Verify:

```text
Logical cell: 1 world unit
Player collider baseline: Capsule 0.72 × 0.90
2-cell height: reachable
Same-level 2-cell gap: pass
Same-level 3-cell gap: fail for legal basic movement, including coyote/buffer timing
Forbidden movement: wall jump / dash / double jump absent
Basic attack/melee/shoot: absent
```

### 3. Course Fixture and Determinism

Verify:

- Movement courses use 1 unit logical cells.
- Course fixtures use the locked runtime capsule.
- 2-cell and 3-cell gap courses differ only in gap width and intended assertion.
- Test inputs are deterministic and fixed-step.
- Results are reproducible across repeated runs.
- There is no hardcoded trajectory or pass/fail result.

### 4. Regression Tests

Run:

```text
Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Expected minimum tests: 57
Expected result: PASS
PlayMode: NOT RUN unless needed for diagnosis
```

The expected minimum is previous 52 plus at least 5 repair regressions.

### 5. Scope and Dependency

Verify no out-of-scope change was introduced:

- Runtime changes are limited to `Assets/_Game/Character/Runtime/Movement/`.
- Test changes are limited to `Assets/_Game/Tests/EditMode/Character/`.
- No MAP, Tilemap, Scene, Prefab, inputactions, asmdef, Packages, ProjectSettings, or MapDesign changes.
- No CHAR03 implementation started.

### 6. Dependency Ledger for CHAR03

Record whether these dependencies are ready or deferred:

```text
MAP world query / coordinate conversion
Room boundary detection and readiness gate
Camera room transition policy
Terrain mutation request API
Generated map route integration
```

These remain deferred. They do not block CHAR02 exit if movement grammar is repaired.

## Required Report

Overwrite or replace:

```text
CharacterDesign/MCP/REPORTS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT_RESULT.md
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
MOVEMENT_GRAMMAR_COVERAGE
COYOTE_THREE_CELL_REPAIR
COURSE_FIXTURE_DETERMINISM
FORBIDDEN_FEATURE_SCAN
DEPENDENCY_LEDGER
OUT_OF_SCOPE_FINDINGS
CHAR02 EXIT
CHAR03_01 ENTRY
DONE CONDITIONS
NEXT
```

If all gates pass, include exactly:

```text
STATUS: PASS
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

- [ ] Previous CHAR02_03 FAIL report verified with exact SHA and rejection text.
- [ ] Legal coyote delayed jump failure reproduced before repair.
- [ ] Repair strategy documented under CHANGE_CONTROL.
- [ ] 2-cell height remains reachable.
- [ ] Same-level 2-cell gap still passes.
- [ ] Same-level 3-cell gap fails for normal and coyote/buffer timing.
- [ ] Coyote delay sweep regression added or updated.
- [ ] Forbidden movement absence still passes.
- [ ] Character EditMode tests pass with at least 57 tests.
- [ ] Unity compile errors 0.
- [ ] Scope validation completed.
- [ ] Dependency ledger completed.
- [ ] CHAR02 EXIT decision recorded with exact required text.
- [ ] CHAR03_01 ENTRY decision recorded with exact required text.
- [ ] No status/master edits made by task execution.
- [ ] CHAR03_01 remains locked.

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
