# RUN MAP08_06

아래 패치를 적용한 뒤 `MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES`만 실행하세요.

```text
Required prior Result:
MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT_RESULT.md
STATUS: PASS
SHA-256: ff8e384a5d450d356017cb91ad52a25b1143931a823c48b67023a7b6be599ac0

Required prior Task SHA-256:
7336541b62db64f0a4e40c2a892a6a07000fa011c87934a68fb9f785c10842a6
```

## Apply

```text
Replace MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
Replace MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
Create  MapDesign/MCP/TASKS/MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES.md
```

## Run Only

```text
Current Task:
MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES
```

## Do Not Start

```text
MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES
MAP08_08~MAP08_14
MAP09+
```

## Required Result Header

```text
TASK: MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES
STATUS: PASS | FAIL | BLOCKED
MAP08_06: COMPLETE ELIGIBLE | REPAIR REQUIRED | BLOCKED
MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES: LOCKED / DO NOT START
```

## Required State After Apply

```text
205 rows
96 COMPLETE
1 CURRENT
108 LOCKED
Current Task: MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES
MAP08 Phase: MAP08_01~MAP08_05 COMPLETE / MAP08_06 CURRENT / MAP08_07~MAP08_14 LOCKED
```

## Commit Requirement

```text
After implementation PASS:
- stage only MAP08_06-owned Authoring CSV, Runtime/Test, MCP, and Result files
- create one git commit with detailed implementation and validation body
- do not stage unrelated pre-existing worktree files
- do not git push
```
