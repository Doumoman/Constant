# RUN CHAR05_04

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
10. CHAR05_03 PASS REPORT와 source registry
11. current Character runtime/test code
12. legacy run-state/HUD/presentation examples는 read-only reference로만 확인
13. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR05_03 Result STATUS: PASS
CHAR05_03 Result SHA-256: d982d596a0efad856db4e8dbaf475538172b9ac8ab11baf4af85bb87b982c03c
CHAR05_03 Task SHA-256: 4d7750096baf950b4c94dc72a76d0c21be64847b34580960db7fa8fa311e52f0
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR05_04 Task SHA-256: 053ecb3e0f0ae02d3c729dc4bf8dcd5ee3247f1e3b2ff95da641fb76898e888b
Status payload SHA-256: e492b814246acfa3277f1b532768d87dbf136ccdea4951964b102c9b2cadb3f6
Master payload SHA-256: 9d78258495b3f81b08f8942a0fdf62b2dfa1dd8719b2534a3abdfa38f7a982a7
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR05_05 이후 Task body는 읽거나 시작하지 않는다.

Required gates:

```text
CHAR05_03SurvivalContractVerified = PASS
RunInventoryState = PASS
RunStatusAndHealthSnapshot = PASS
HudSnapshotBridge = PASS
PresentationEventRequests = PASS
EventOrderingAndDeduplication = PASS
AuthorityAndForbiddenFeatureGuard = PASS
TargetedEditModeTests = PASS, minimum 158 tests
UnityCompile = PASS
ScopeValidation = PASS
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE_RESULT.md
```

모든 gate가 PASS일 때만 CHAR05_04를 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR05_05는 LOCKED로 유지하고 자동 시작하지 않는다.
