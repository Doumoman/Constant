# RUN MAP08_11

아래 패치를 적용한 뒤 `MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES`만 실행하세요.

```text
Required prior Result:
MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES_RESULT.md
STATUS: PASS
SHA-256: 058b5ed32dbca7ca06adf12595ac693f5218dec5f8ed0bfe14d5c12c03563f5a

Required prior installed Task SHA-256:
f8050e209aa342602616afadc7cf0c3258731c8034ab9aff94f2bd94918d04f8
```

## Apply

```text
Replace MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
Replace MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
Create  MapDesign/MCP/TASKS/MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES.md
```

## Run Only

```text
Current Task:
MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES
```

## Do Not Start

```text
MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR
MAP08_13~MAP08_14
MAP09+
```

## Required Result Header

```text
TASK: MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES
STATUS: PASS | FAIL | BLOCKED
MAP08_11: COMPLETE ELIGIBLE | REPAIR REQUIRED | BLOCKED
MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR: LOCKED / DO NOT START
```

## Required State After Apply

```text
205 rows
101 COMPLETE
1 CURRENT
103 LOCKED
Current Task: MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES
MAP08 Phase: MAP08_01~MAP08_10 COMPLETE / MAP08_11 CURRENT / MAP08_12~MAP08_14 LOCKED
```

## Commit Requirement

```text
After implementation PASS:
- stage only MAP08_11-owned Authoring CSV, Runtime/Test, MCP, Result, and finalized Status files
- create one git commit with detailed implementation and validation body
- report the exact commit hash in the final handoff
- do not stage unrelated pre-existing worktree files
- do not git push
```

