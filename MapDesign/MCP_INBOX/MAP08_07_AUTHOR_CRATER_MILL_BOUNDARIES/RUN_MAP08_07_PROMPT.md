# RUN MAP08_07

아래 패치를 적용한 뒤 `MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES`만 실행하세요.

```text
Required prior Result:
MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES_RESULT.md
STATUS: PASS
SHA-256: 618cec23763ab38d4053a30ae348a4d6c187e2a8d4587d786247a514956a2ece

Required prior installed/repaired Task SHA-256:
24799620d254846e3a99c0a3dadcb00156ab3f6f4804bbf09d2ce4eacda70293
```

## Apply

```text
Replace MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
Replace MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
Create  MapDesign/MCP/TASKS/MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES.md
```

## Run Only

```text
Current Task:
MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES
```

## Do Not Start

```text
MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES
MAP08_09~MAP08_14
MAP09+
```

## Required Result Header

```text
TASK: MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES
STATUS: PASS | FAIL | BLOCKED
MAP08_07: COMPLETE ELIGIBLE | REPAIR REQUIRED | BLOCKED
MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES: LOCKED / DO NOT START
```

## Required State After Apply

```text
205 rows
97 COMPLETE
1 CURRENT
107 LOCKED
Current Task: MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES
MAP08 Phase: MAP08_01~MAP08_06 COMPLETE / MAP08_07 CURRENT / MAP08_08~MAP08_14 LOCKED
```

## Commit Requirement

```text
After implementation PASS:
- stage only MAP08_07-owned Authoring CSV, Runtime/Test, MCP, and Result files
- create one git commit with detailed implementation and validation body
- do not stage unrelated pre-existing worktree files
- do not git push
```
