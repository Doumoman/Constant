# RUN MAP09_00

아래 패치를 `MapDesign/MCP_INBOX/`에 패치 폴더째 넣은 뒤 `MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md`를 수행하세요.

## Required Prior Gate

```text
MAP08_14_MAP08_EXIT_TESTS_RESULT.md
STATUS: PASS
SHA-256: 5d0b2f0d478ef8479b93e1b9163445f6e736022b533dee77f81690b8670cf2d1

Installed MAP08_14 Task SHA-256:
6fffc0ed3f8ca333cf7d74d44c437ab6e4193871ce8b2a7a254405e4bcaa5e8e
```

## Apply

```text
Replace MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
Replace MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
Create  MapDesign/MCP/TASKS/MAP09_00_CREATE_V2_MODULE_STRUCTURE.md
```

## Run Only

```text
Current Task:
MAP09_00_CREATE_V2_MODULE_STRUCTURE
```

## Do Not Start

```text
MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES
Retired MAP09_01_IMPLEMENT_SECTOR_RECIPE_RESOLVER
MAP09_01+
```

## Required Result Header

```text
TASK: MAP09_00_CREATE_V2_MODULE_STRUCTURE
STATUS: PASS | FAIL | BLOCKED
MAP09_00: COMPLETE ELIGIBLE only if PASS
V2 MODULE STRUCTURE: APPROVED only if PASS
MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES: LOCKED / DO NOT START
```

## Required State After Apply

```text
214 rows
105 COMPLETE
1 CURRENT
108 LOCKED
Current Task: MAP09_00_CREATE_V2_MODULE_STRUCTURE
MAP09_01 and later: LOCKED
```

## Commit Requirement

```text
After implementation PASS:
- stage only the MAP09_00 patch, exact new folder metas, Result, and status finalize
- create one git commit with the exact Task subject and detailed validation body
- do not stage unrelated pre-existing worktree files
- do not git push
```
