# RUN MAP08_08

아래 패치를 적용한 뒤 `MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES`만 실행하세요.

```text
Required prior Result:
MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES_RESULT.md
STATUS: PASS
SHA-256: 59cc98beaa374a319c656c50f0c5aaf26a4f25a29f591eb017bf21d4a9eb995a

Required prior installed/repaired Task SHA-256:
bf9085abb16be5c0bc736fa78b709fd32972f5903ba332622860d41d13aa4577
```

## Apply

```text
Replace MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
Replace MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
Create  MapDesign/MCP/TASKS/MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES.md
```

## Run Only

```text
Current Task:
MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES
```

## Do Not Start

```text
MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES
MAP08_10~MAP08_14
MAP09+
```

## Required Result Header

```text
TASK: MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES
STATUS: PASS | FAIL | BLOCKED
MAP08_08: COMPLETE ELIGIBLE | REPAIR REQUIRED | BLOCKED
MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES: LOCKED / DO NOT START
```

## Required State After Apply

```text
205 rows
98 COMPLETE
1 CURRENT
106 LOCKED
Current Task: MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES
MAP08 Phase: MAP08_01~MAP08_07 COMPLETE / MAP08_08 CURRENT / MAP08_09~MAP08_14 LOCKED
```

## Commit Requirement

```text
After implementation PASS:
- stage only MAP08_08-owned Authoring CSV, Runtime/Test, MCP, and Result files
- create one git commit with detailed implementation and validation body
- do not stage unrelated pre-existing worktree files
- do not git push
```
