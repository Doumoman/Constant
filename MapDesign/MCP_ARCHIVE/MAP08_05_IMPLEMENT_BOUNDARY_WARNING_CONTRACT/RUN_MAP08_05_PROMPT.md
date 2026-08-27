# RUN MAP08_05

아래 패치를 적용한 뒤 `MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT`만 실행하세요.

```text
Required prior Result:
MAP08_04_FILTER_MANDATORY_BOUNDARIES_RESULT.md
STATUS: PASS
SHA-256: f189dc539efd54979d376d6bba5c809aadf93e7c63098d81ca0acd0656a7a4fd

Required prior Task SHA-256:
9e3992c4bcf5ff7f891e236bfddeb00f746c6fee3effd2fb8a18b18da08781b9
```

## Apply

```text
Replace MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
Replace MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
Create  MapDesign/MCP/TASKS/MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT.md
```

## Run Only

```text
Current Task:
MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT
```

## Do Not Start

```text
MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES
MAP08_07~MAP08_14
MAP09+
```

## Required Result Header

```text
TASK: MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT
STATUS: PASS | FAIL | BLOCKED
MAP08_05: COMPLETE ELIGIBLE | REPAIR REQUIRED | BLOCKED
MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES: LOCKED / DO NOT START
```

## Required State After Apply

```text
205 rows
95 COMPLETE
1 CURRENT
109 LOCKED
Current Task: MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT
MAP08 Phase: MAP08_01~MAP08_04 COMPLETE / MAP08_05 CURRENT / MAP08_06~MAP08_14 LOCKED
```

## Commit Requirement

```text
After implementation PASS:
- stage only MAP08_05-owned implementation/test/result files
- create one git commit with detailed implementation and validation body
- do not stage unrelated pre-existing worktree files
- do not git push
```
