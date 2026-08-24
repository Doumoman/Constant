# TASK: CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD

```yaml
task_id: CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD
phase: CHAR06_GENERATED_MAP_AND_FINAL_VALIDATION
task_type: VALIDATION_ONLY
created: 2026-08-25
workflow: MCP_INBOX_PATCH_ONLY
write_scope: REPORT_AND_VALIDATION_ARTIFACTS_ONLY
```

## Objective

Run the full Unity validation gate for the completed Character runtime.

This task owns:

```text
Unity compile validation
Character EditMode validation
available MAP and Character EditMode regression validation
available PlayMode validation
active build target validation
console error audit
validation artifact and report recording
no gameplay implementation, no runtime rewrite, no MAP rewrite, no test hiding, no build-result manipulation
```

This task must not perform final documentation and commit audit. `CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT` owns the final EXIT audit.

## Entry Gate

Before running validation, verify:

```text
Current Task: TASKS/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD.md
CHAR06_02 Result: PASS
CHAR06_02 Result SHA-256: 9ae578c70b7062ce7285ac75c2ec35689ee4c4246ae50d89ce42496fd60e37ab
CHAR06_02 contains: 177/177 PASS
CHAR06_02 contains: Current Task after finalize: NONE
CHAR06_02 contains: CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD
CHAR06_02 contains: LOCKED 유지
Source Registry marker: REGISTRY_STATE: FILLED_BY_CHAR00_01
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR06_04: LOCKED
```

If any entry gate is false, write a BLOCKED report and do not run destructive validation or modify project code.

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
10. `CharacterDesign/MCP/TASKS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS.md`
11. `CharacterDesign/MCP/REPORTS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS_RESULT.md`
12. `CharacterDesign/MCP/REPORTS/CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES_RESULT.md`
13. `CharacterDesign/MCP/REPORTS/CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT_RESULT.md`
14. `CharacterDesign/01_FIXED_SPEC/01_CHARACTER_GAMEPLAY_RULES.md`
15. `CharacterDesign/01_FIXED_SPEC/06_CHARACTER_MAP_INTEGRATION_RULES.md`
16. `CharacterDesign/01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
17. Current character runtime under `Assets/_Game/Character/Runtime/`
18. Current character tests under `Assets/_Game/Tests/EditMode/Character/` and `Assets/_Game/Tests/PlayMode/Character/` if present
19. Current MAP tests under `Assets/_Game/Tests/EditMode/Map/` and `Assets/_Game/Tests/PlayMode/Map/` if present
20. `Packages/manifest.json`
21. Unity project version and active build target information available through Unity MCP or project metadata

Do not read or start the `CHAR06_04` task body.

## Allowed Writes

Allowed writes:

```text
CharacterDesign/MCP/REPORTS/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD_RESULT.md
CharacterDesign/MCP/REPORTS/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD_ARTIFACTS/**
Builds/Validation/CHAR06_03/**
Temp/CharacterValidation/CHAR06_03/**
```

The `Builds/Validation/CHAR06_03/**` path is for disposable player build output only. Do not add or commit that output unless the user explicitly asks.

Forbidden writes:

```text
Assets/**
Packages/**
ProjectSettings/**
MapDesign/**
CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md
CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
CharacterDesign/MCP/TASKS/**
```

Forbidden:

- Runtime code changes.
- Test code changes.
- asmdef changes.
- Scene, prefab, physics layer asset, inputactions, Packages, ProjectSettings, MAP runtime, MAP authoring data, Tilemap, camera, animation, audio, UI, save data, or legacy code changes.
- Test count reduction, Ignore insertion, test-result editing, console-log filtering that hides errors, or build-result manipulation.
- Switching build targets if it requires installing modules or changing project settings.
- Opening or installing any future task.
- Committing before writing and validating the result report.

If validation cannot pass without forbidden writes, write `STATUS: BLOCKED` and record the exact blocker.

## Required Validation

### 1. Unity Compile Clean

Required behavior:

```text
refresh Unity project
wait for compile to finish
collect compiler diagnostics
compile errors must be 0
record Unity version
record active build target
record compile warning summary if available
```

### 2. EditMode Test Validation

Required behavior:

```text
run Game.Character.Tests.EditMode
Character EditMode expected minimum tests: 177
Character EditMode expected result: PASS
run available MAP EditMode regression assemblies if they compile in the current project
record every EditMode assembly name, test count, pass, fail, skip, duration, and result file path if Unity emits one
do not ignore or filter failing tests
```

If MAP EditMode validation is blocked by an unrelated stale reference, report the exact assembly and reference name. Do not edit MAP asmdefs in this task.

### 3. PlayMode Test Validation

Required behavior:

```text
discover available PlayMode test assemblies
run all available PlayMode tests that Unity can discover for the current project
if no PlayMode tests exist, record zero discovered PlayMode tests and treat discovery as successful only if Unity reports no compile or discovery errors
if PlayMode discovery or compile fails, record STATUS: FAIL or STATUS: BLOCKED with the exact assembly, missing reference, or environment issue
```

Do not create PlayMode tests in this task.

### 4. Build Validation

Required behavior:

```text
validate the currently active build target
write any generated player build output only under Builds/Validation/CHAR06_03
do not switch platform if it requires module installation or ProjectSettings changes
if the active target cannot build because a module, signing key, external SDK, or environment dependency is missing, record STATUS: BLOCKED with the exact missing dependency
if build runs, record result, output path, duration, warnings, and errors
```

Build success is required for `STATUS: PASS`.

### 5. Console and Scope Audit

Required behavior:

```text
new console errors during validation must be 0
source file changes must be 0
Assets, Packages, ProjectSettings, MapDesign, inputactions, scenes, prefabs, Tilemap assets, UI, audio, save, and legacy code must remain unchanged
pre-existing dirty files must be recorded separately and not touched
CHAR06_04 remains locked
```

## Required Report

Write:

```text
CharacterDesign/MCP/REPORTS/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD_RESULT.md
```

The report must include:

- `TASK`
- independent line `STATUS: PASS`, `STATUS: FAIL`, or `STATUS: BLOCKED`
- `SUMMARY`
- `READ`
- `CHANGED`
- `CREATED`
- `UNITY_COMPILE`
- `EDITMODE`
- `PLAYMODE`
- `BUILD`
- `CONSOLE_AUDIT`
- `SCOPE_VALIDATION`
- `REGRESSION_SUMMARY`
- `DEPENDENCY_LEDGER`
- `OUT_OF_SCOPE_FINDINGS`
- `DONE CONDITIONS`
- `NEXT`

Required report facts:

```text
Entry gate verification result
CHAR06_02 report hash used
source registry hash used
Unity version
active build target
compile error count
Character EditMode test count and pass/fail/skip count
all MAP EditMode assemblies attempted and their results
all PlayMode assemblies attempted and their results
build target, output path, success/failure, duration, errors, and missing dependency if any
new console error count
all files changed and created
pre-existing dirty files not touched
confirmation that Assets, Packages, ProjectSettings, MapDesign, scenes, prefabs, inputactions, UI, audio, save, legacy, and MAP runtime were not modified
confirmation that CHAR06_04 remains locked
```

PASS requires compile errors 0, Character EditMode passing with at least 177 tests, all attempted regression tests passing, build success, new console errors 0, and no scope violations.

## Completion and Finalization Rule

If PASS:

```text
Finalize CHAR06_03 as COMPLETE.
Set Current Task after finalize: NONE.
Do not auto-open CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT.
Keep CHAR06_04 LOCKED until a new MCP_INBOX package opens CHAR06_04.
```

If FAIL or BLOCKED:

```text
Keep CHAR06_03 as CURRENT.
Do not open CHAR06_04.
Report exact compile error, test failure, PlayMode discovery failure, build failure, missing module, or scope violation.
```

