# RUN CHAR05_05

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
10. CHAR05_01/02/03/04 PASS REPORT와 source registry
11. current Character runtime/test code
12. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR05_04 Result STATUS: PASS
CHAR05_04 Result SHA-256: 321877eda8f80333bb285abd9d850cd7d9a44577ac85dfc53515d7a47331572c
CHAR05_04 Task SHA-256: 053ecb3e0f0ae02d3c729dc4bf8dcd5ee3247f1e3b2ff95da641fb76898e888b
CHAR05_03 Result SHA-256: d982d596a0efad856db4e8dbaf475538172b9ac8ab11baf4af85bb87b982c03c
CHAR05_02 Result SHA-256: 940e5cf9909cc55a6562704c530ee7abba2d9638ac52627d9b0146922cb98fef
CHAR05_01 Result SHA-256: 1c5036404d957cc5ca534d4c0ec89e77995c3d6adfa66306b73915bc42005e7f
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR05_05 Task SHA-256: b740d76985ef294defc53c04d885d57ef64cc833674cbb1313a259d6850531f6
Status payload SHA-256: 19fedbbec2a223c8b39abb5ea185d194e6b6020ba4f403912a815721841117f9
Master payload SHA-256: 8ecdfd978056ac7053e7561e6d6abb77e9e0ad29b83b5df04b3714ab45cecd66
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR06 Task body는 읽거나 시작하지 않는다.

Required gates:

```text
CHAR05PhaseLedger = PASS
BombTerrainRequestAudit = PASS
RopeTraversalAudit = PASS
SurvivalRunFailureAudit = PASS
RunStatePresentationAudit = PASS
ForbiddenFeatureGuard = PASS
DependencyDirection = PASS
TargetedEditModeTests = PASS, minimum 158 tests
UnityCompile = PASS
ScopeValidation = PASS, report-only
CHAR05_EXIT_DECISION = APPROVED
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT_RESULT.md
```

모든 gate가 PASS일 때만 CHAR05_05를 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR06_01은 LOCKED로 유지하고 자동 시작하지 않는다.
