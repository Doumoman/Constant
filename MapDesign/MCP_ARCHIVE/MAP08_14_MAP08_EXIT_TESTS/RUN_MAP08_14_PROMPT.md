# RUN MAP08_14

아래 패치를 적용한 뒤 `MAP08_14_MAP08_EXIT_TESTS`만 실행하세요.

```text
Required prior Result:
MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW_RESULT.md
STATUS: PASS
SHA-256: cb72264380c94a35ab6abe42f672c06e994f30deadc5a867546a31279b9bf7cd

Required prior installed Task SHA-256:
5e932e82ae7ed78e732c26907ac4cd480e43d7bf14ee9978df46d2917310153d
```

## Apply

```text
Replace MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
Replace MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
Create  MapDesign/MCP/TASKS/MAP08_14_MAP08_EXIT_TESTS.md
```

## Run Only

```text
Current Task:
MAP08_14_MAP08_EXIT_TESTS
```

## Do Not Start

```text
MAP09_01_IMPLEMENT_SECTOR_RECIPE_RESOLVER
MAP09+
```

## Required Result Header

```text
TASK: MAP08_14_MAP08_EXIT_TESTS
STATUS: PASS | FAIL | BLOCKED
MAP08_14: COMPLETE ELIGIBLE | REPAIR REQUIRED | BLOCKED
MAP08 PHASE EXIT: APPROVED only if PASS
MAP09_01_IMPLEMENT_SECTOR_RECIPE_RESOLVER: LOCKED / DO NOT START
```

## Required State After Apply

```text
205 rows
104 COMPLETE
1 CURRENT
100 LOCKED
Current Task: MAP08_14_MAP08_EXIT_TESTS
MAP08 Phase: MAP08_01~MAP08_13 COMPLETE / MAP08_14 CURRENT
MAP09_01 and later: LOCKED
```

## Commit Requirement

```text
After implementation PASS:
- stage only MAP08_14-owned Test, MCP, and Result files
- create one git commit with detailed implementation and validation body
- include created commit hash in the Result
- do not stage unrelated pre-existing worktree files
- do not git push
```
