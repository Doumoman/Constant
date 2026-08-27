# RUN MAP08_13

아래 패치를 적용한 뒤 `MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW`만 실행하세요.

```text
Required prior Result:
MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR_RESULT.md
STATUS: PASS
SHA-256: 26801f7bc31d354b9639278ec133970e7840f3c7dacb9f0f2a4b6a0e0288896b

Required prior installed Task SHA-256:
cdaadd203562acdcad7024349499a2df3ac0ccd9860ef580473e984ae9490966

External accounted Result:
MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES_RESULT.md
STATUS: PASS
SHA-256: 058b5ed32dbca7ca06adf12595ac693f5218dec5f8ed0bfe14d5c12c03563f5a
```

## Apply

```text
Replace MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
Replace MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
Create  MapDesign/MCP/TASKS/MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW.md
```

## Run Only

```text
Current Task:
MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW
```

## Do Not Start

```text
MAP08_14_MAP08_EXIT_TESTS
MAP09+
```

## Required Result Header

```text
TASK: MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW
STATUS: PASS | FAIL | BLOCKED
MAP08_13: COMPLETE ELIGIBLE | REPAIR REQUIRED | BLOCKED
MAP08_14_MAP08_EXIT_TESTS: LOCKED / DO NOT START
```

## Required State After Apply

```text
205 rows
103 COMPLETE
1 CURRENT
101 LOCKED
Current Task: MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW
MAP08 Phase: MAP08_01~MAP08_12 COMPLETE / MAP08_13 CURRENT / MAP08_14 LOCKED
```

## Commit Requirement

```text
After implementation PASS:
- stage only MAP08_13-owned Editor/Test, MCP, and Result files
- create one git commit with detailed implementation and validation body
- include created commit hash in the Result
- do not stage unrelated pre-existing worktree files
- do not git push
```
