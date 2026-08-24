# RUN CHAR01_04

`CharacterDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md`의 Phase A/B/C를 수행한다.

읽기 순서:

1. `MCP/00_MCP_ENTRYPOINT.md`
2. `MCP/01_CHARACTER_LOCKED_RULES.md`
3. `MCP/02_MCP_WORK_RULES.md`
4. `MCP/03_CHARACTER_DATA_RULES.md`
5. `MCP/04_UNITY_MCP_RULES.md`
6. `MCP/05_CHANGE_CONTROL_RULES.md`
7. `MCP/07_PATCH_APPLY_RULES.md`
8. 이 package의 `PATCH_MANIFEST.md`
9. patch 적용 후 Master, Status, Current Task
10. CHAR01_01~03 PASS REPORT와 source registry
11. current Character runtime/test code와 fixed specs/schemas/fixtures
12. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR01_03 Result STATUS: PASS
CHAR01_03 Result SHA-256: 373fb206c50790fc99add891783f99bc969a67273da26e6dbd906ea108cad5d2
CHAR01_03 Task SHA-256: 4f28c237637c9ace93e87250240cd61d1c8db9cbb384ed5ea5d038e5bdf9b99d
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR01_04 Task SHA-256: ce1f06036b4b75d44af17eb30ede14f69d148b9c097ef6dc691fd8fa1e4f2837
Status payload SHA-256: 9e15f7a344ead6d5840fe507b6a53419009da4a7e9dbfc193692993576f261dc
Master payload SHA-256: 38dff006b1fee0f86a88d131f4e29fcd500246131920f64a909a47e916dd1925
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR02_01 이후 Task body는 읽거나 시작하지 않는다.

Required Task gates:

```text
PriorEvidenceAndState = PASS
RuntimeAssemblyAndBoundary = PASS
CoreMovementContractCoverage = PASS
RegressionTests = PASS (36/36)
ForbiddenFeatureAndScope = PASS
DependencyLedger = PASS
CHAR01ExitDecision = PASS
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT_RESULT.md
```

모든 gate가 PASS일 때만 REPORT에 `CHAR01 EXIT: APPROVED`, `CHAR02_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH`를 기록하고 CHAR01_04를 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR02_01은 LOCKED로 유지하고 자동 시작하지 않는다.
