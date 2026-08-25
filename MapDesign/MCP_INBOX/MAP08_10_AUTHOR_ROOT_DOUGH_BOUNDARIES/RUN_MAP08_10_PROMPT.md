# RUN MAP08_10

아래 패치를 적용한 뒤 `MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES`만 실행하세요.

```text
Required prior Result:
MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES_RESULT.md
STATUS: PASS
SHA-256: c923b445d9dc7b1d057ec368cb154a5745f2e473e67a736fcf8ee20f66a9ef87

Required prior installed Task SHA-256:
c559168ad86e866124cc87843926d66b98f59ad1243a33755fce7f78621fd01f
```

## Apply

```text
Replace MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
Replace MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
Create  MapDesign/MCP/TASKS/MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES.md
```

## Run Only

```text
Current Task:
MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES
```

## Do Not Start

```text
MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES
MAP08_12~MAP08_14
MAP09+
```

## Required Result Header

```text
TASK: MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES
STATUS: PASS | FAIL | BLOCKED
MAP08_10: COMPLETE ELIGIBLE | REPAIR REQUIRED | BLOCKED
MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES: LOCKED / DO NOT START
```

## Required State After Apply

```text
205 rows
100 COMPLETE
1 CURRENT
104 LOCKED
Current Task: MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES
MAP08 Phase: MAP08_01~MAP08_09 COMPLETE / MAP08_10 CURRENT / MAP08_11~MAP08_14 LOCKED
```

## Commit Requirement

```text
After implementation PASS:
- stage only MAP08_10-owned Authoring CSV, Runtime/Test, MCP, and Result files
- create one git commit with detailed implementation and validation body
- include created commit hash in the Result
- do not stage unrelated pre-existing worktree files
- do not git push
```
