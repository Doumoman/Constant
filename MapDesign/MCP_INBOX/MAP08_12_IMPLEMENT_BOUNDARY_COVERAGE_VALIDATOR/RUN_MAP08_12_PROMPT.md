# RUN MAP08_12

아래 패치를 적용한 뒤 `MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR`만 실행하세요.

```text
Required prior Result:
MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES_RESULT.md
STATUS: PASS
SHA-256: 9c9ce342563858987b2489ae6aa9a50bee2473be4639b07cd7176ee18bcbde4c

Required prior installed Task SHA-256:
MUST COMPUTE FROM MapDesign/MCP/TASKS/MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES.md
```

## Apply

```text
Replace MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
Replace MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
Create  MapDesign/MCP/TASKS/MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR.md
```

## Run Only

```text
Current Task:
MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR
```

## Do Not Start

```text
MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW
MAP08_14_MAP08_EXIT_TESTS
MAP09+
```

## Required Result Header

```text
TASK: MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR
STATUS: PASS | FAIL | BLOCKED
MAP08_12: COMPLETE ELIGIBLE | REPAIR REQUIRED | BLOCKED
MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW: LOCKED / DO NOT START
```

## Required State After Apply

```text
205 rows
102 COMPLETE
1 CURRENT
102 LOCKED
Current Task: MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR
MAP08 Phase: MAP08_01~MAP08_11 COMPLETE / MAP08_12 CURRENT / MAP08_13~MAP08_14 LOCKED
```

## Commit Requirement

```text
After implementation PASS:
- stage only MAP08_12-owned Runtime/Test, MCP, and Result files
- create one git commit with detailed implementation and validation body
- include created commit hash in the Result
- do not stage unrelated pre-existing worktree files
- do not git push
```
