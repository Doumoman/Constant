# RUN MAP08_09

아래 패치를 적용한 뒤 `MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES`만 실행하세요.

```text
Required prior Result:
MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES_RESULT.md
STATUS: PASS
SHA-256: df4137defbb8e44cba12ef3b74cd8635044b886657525ec128e05dd5b1bd67c9

Required prior installed Task SHA-256:
92106729b49fa13c0fbb95f0338b3b619582a1696f10ac88604401469907d769
```

## Apply

```text
Replace MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
Replace MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
Create  MapDesign/MCP/TASKS/MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES.md
```

## Run Only

```text
Current Task:
MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES
```

## Do Not Start

```text
MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES
MAP08_11~MAP08_14
MAP09+
```

## Required Result Header

```text
TASK: MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES
STATUS: PASS | FAIL | BLOCKED
MAP08_09: COMPLETE ELIGIBLE | REPAIR REQUIRED | BLOCKED
MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES: LOCKED / DO NOT START
```

## Required State After Apply

```text
205 rows
99 COMPLETE
1 CURRENT
105 LOCKED
Current Task: MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES
MAP08 Phase: MAP08_01~MAP08_08 COMPLETE / MAP08_09 CURRENT / MAP08_10~MAP08_14 LOCKED
```

## Commit Requirement

```text
After implementation PASS:
- stage only MAP08_09-owned Authoring CSV, Runtime/Test, MCP, and Result files
- create one git commit with detailed implementation and validation body
- include created commit hash in the Result
- do not stage unrelated pre-existing worktree files
- do not git push
```
