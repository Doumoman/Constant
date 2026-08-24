# RUN CHAR03_01 REPAIR

`CharacterDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md`의 Phase A/B/C를 수행한다.

이 패키지는 일반 next-task open package가 아니라 실패한 current task의 change-control repair package다.

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
10. CHAR03_01 BLOCKED REPORT
11. CHAR02_03 PASS REPORT와 source registry
12. current Character runtime/test code
13. MAP public coordinate/domain runtime
14. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
Current Task before patch: TASKS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE.md
CHAR03_01 Previous Result STATUS: BLOCKED
CHAR03_01 Previous Result SHA-256: b4e37ef7dd56fc1a081969619ace9b25b4edd62f9cef4167cd0bd88ded9e963f
CHAR03_01 Previous Task SHA-256: e4127a04a3b75840650bba788cf606c13370c05879674f5e5403eca9a7ef91a5
CHAR02_03 Result SHA-256: e118ac9d286252bad58387e2675b32d6eee38abf7f592ecb06b6d591d6370fb5
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
Revised CHAR03_01 Task SHA-256: e6cd5601cdcb25511dc3e61f08353b1b2310ee66c4fd7a63aa0599566194f1fc
Status payload SHA-256: 6f474711a60b488def706b077621e183f694250d5752e33eb12ae6dae3d76d8f
Master payload SHA-256: 1ce2aca14f9b48474a2f63b29915295baed7ee751b8a6948112414378e3947f8
```

Required gates:

```text
PreviousBlockedReportVerified = PASS
DependencyGuardRepair = PASS
MapCoordinateBridge = PASS
MapWorldQueryContract = PASS
RoomBoundaryReadinessGate = PASS
DependencyDirectionGuard = PASS
TargetedEditModeTests = PASS, minimum 66 tests
UnityCompile = PASS
ScopeValidation = PASS
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE_RESULT.md
```

모든 gate가 PASS일 때만 CHAR03_01을 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR03_02는 LOCKED로 유지하고 자동 시작하지 않는다.
