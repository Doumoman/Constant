# RUN CHAR03_03

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
10. CHAR03_01/CHAR03_02 PASS REPORT와 source registry
11. current Character runtime/test code
12. MAP public coordinate/domain runtime
13. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR03_02 Result STATUS: PASS
CHAR03_02 Result SHA-256: a99a1ed377aed266632ee1da2245610cbcc97015a67af23bc31ac3fc81092082
CHAR03_02 Task SHA-256: bee0ef965f6aeeb7505eb26e2b9274d27102fc68d879c6394301ce3651860a32
CHAR03_01 Result SHA-256: 3a3009d76b6b89e5bae44b6d743f866b6209728c3509e22b3ad7332063b9317b
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR03_03 Task SHA-256: 644919d9843a92333a4ba7fb069ffd07f3e26ac1c8ffca32fdfc05620c3a690e
Status payload SHA-256: 3a2ef6be38926a8b2535c3e350c1cae69ca83fc01d45cc93c48fd727cb985b41
Master payload SHA-256: 7811fd3e5ead8e0f0f4af51b620b47969327375c2d3a10024d0797c5f34bf2b8
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR04_01 이후 Task body는 읽거나 시작하지 않는다.

Required audit gates:

```text
PriorEvidenceAndState = PASS
MapCoordinateAndQueryAudit = PASS
RoomBoundaryReadinessAudit = PASS
CameraRoomTransitionAudit = PASS
InputVelocityKeepAudit = PASS
HysteresisAndEdgeEntryAudit = PASS
DependencyDirection = PASS
TargetedEditModeTests = PASS, minimum 76 tests
UnityCompile = PASS
ScopeValidation = PASS
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT_RESULT.md
```

모든 gate가 PASS일 때만 CHAR03_03을 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR04_01은 LOCKED로 유지하고 자동 시작하지 않는다.
