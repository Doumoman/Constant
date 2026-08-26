```yaml
mcp_patch:
  format: single_task_v1
  task_id: <TASK_ID>
  task_file: TASKS/<TASK_ID>.md
  requires_current_task: NONE
  requires_completed_task: <PREVIOUS_TASK_ID>
  requires_result:
    path: REPORTS/<PREVIOUS_TASK_ID>_RESULT.md
    status: PASS
    sha256: <64-lowercase-hex>
  requires_installed_task:
    path: TASKS/<PREVIOUS_TASK_ID>.md
    sha256: <64-lowercase-hex>
  sets_current_task: <TASK_ID>
```

# <TASK_ID> - <TASK_TITLE>

```yaml
status_control:
  task_key: <TASK_ID>
  result_file: REPORTS/<TASK_ID>_RESULT.md
```

## TASK TYPE

```text
<TASK_TYPE>
```

## Objective

<OBJECTIVE>

## Mandatory Read Order

1. `00_MCP_ENTRYPOINT.md`
2. `01_PROJECT_LOCKED_RULES.md`
3. `02_MCP_WORK_RULES.md`
4. `05_CHANGE_CONTROL_RULES.md`
5. `07_PATCH_APPLY_RULES.md`
6. `08_STATUS_FINALIZE_RULES.md`
7. `APPLY_PATCH_AND_RUN_CURRENT_TASK.md`
8. `MASTER_IMPLEMENTATION_TASK_LIST.md`
9. `06_IMPLEMENTATION_STATUS.md`
10. this installed Task
11. `<ADDITIONAL_REQUIRED_INPUT>`

## READ ALLOWLIST

```text
<READ_PATH_OR_INVENTORY>
```

## WRITE ALLOWLIST

```text
<WRITE_PATH>
REPORTS/<TASK_ID>_RESULT.md
```

Task Execution must not modify `MapDesign/MCP/06_IMPLEMENTATION_STATUS.md`.

## Requirements

1. <REQUIREMENT_1>
2. <REQUIREMENT_2>

## Tests and Static Gates

```text
<FOCUSED_TEST>
<REGRESSION_TEST>
<STATIC_GATE>
```

## Result Report Requirements

Create `REPORTS/<TASK_ID>_RESULT.md` with this header:

```text
TASK: <TASK_ID>
STATUS: PASS | FAIL | BLOCKED
<TASK_GATE>: APPROVED only if PASS
<NEXT_TASK_ID>: LOCKED / DO NOT START
```

Report implementation, tests, compile/Console checks, static gates, changed paths, out-of-scope findings, and the next-task lock state.

## Done Conditions

- [ ] <DONE_CONDITION_1>
- [ ] <DONE_CONDITION_2>
- [ ] Result is PASS before Status Finalize.
- [ ] `<NEXT_TASK_ID>` remains `LOCKED / DO NOT START`.
- [ ] One atomic commit is created after PASS and Status Finalize.
- [ ] No push is performed.

## Next-Task Lock

Do not create, open, execute, or implement `<NEXT_TASK_ID>`. Stop after this Task's PASS-gated Status Finalize and atomic commit.
