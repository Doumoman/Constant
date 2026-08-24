# RUN CHAR03_01

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
10. CHAR02_03 PASS REPORT와 source registry
11. current Character runtime/test code
12. MAP public coordinate/domain runtime
13. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR02_03 Result STATUS: PASS
CHAR02_03 Result SHA-256: e118ac9d286252bad58387e2675b32d6eee38abf7f592ecb06b6d591d6370fb5
CHAR02_03 Task SHA-256: 6c4b7f0a9e047db07d3c3c1b667f6b74e619ddddef7d1c1bafa889da52ad2250
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR03_01 Task SHA-256: e4127a04a3b75840650bba788cf606c13370c05879674f5e5403eca9a7ef91a5
Status payload SHA-256: 9ff8309d5a8e257207a7f850b05b6bd5fba35b0abbc5c9d450b6b39eb1ce9a87
Master payload SHA-256: 985d003919e6e3cae107c6625fde10b70249a73f59b3ef44cd99a1e812a84670
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR03_02 이후 Task body는 읽거나 시작하지 않는다.

Required gates:

```text
CHAR02ExitApproved = PASS
MapCoordinateBridge = PASS
MapWorldQueryContract = PASS
RoomBoundaryReadinessGate = PASS
DependencyDirectionGuard = PASS
TargetedEditModeTests = PASS, minimum 65 tests
UnityCompile = PASS
ScopeValidation = PASS
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE_RESULT.md
```

모든 gate가 PASS일 때만 CHAR03_01을 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR03_02는 LOCKED로 유지하고 자동 시작하지 않는다.
