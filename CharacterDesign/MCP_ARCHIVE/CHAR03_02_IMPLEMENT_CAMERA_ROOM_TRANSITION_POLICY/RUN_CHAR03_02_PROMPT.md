# RUN CHAR03_02

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
10. CHAR03_01 PASS REPORT와 source registry
11. current Character runtime/test code
12. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR03_01 Result STATUS: PASS
CHAR03_01 Result SHA-256: 3a3009d76b6b89e5bae44b6d743f866b6209728c3509e22b3ad7332063b9317b
CHAR03_01 Task SHA-256: e6cd5601cdcb25511dc3e61f08353b1b2310ee66c4fd7a63aa0599566194f1fc
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR03_02 Task SHA-256: bee0ef965f6aeeb7505eb26e2b9274d27102fc68d879c6394301ce3651860a32
Status payload SHA-256: cc45c584d9d830933bdc400dda5673f945285a4c99243788ace2d86ff6dcaca6
Master payload SHA-256: 1affc45f56336d3826a8f7af37ca826461bf0205a59158e1445b8737dd31c564
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR03_03 이후 Task body는 읽거나 시작하지 않는다.

Required gates:

```text
CHAR03_01ReadinessGateVerified = PASS
CameraRoomTransitionPolicy = PASS
InputKeep = PASS
VelocityKeep = PASS
Hysteresis = PASS
HighSpeedAndAirborneEntry = PASS
TargetedEditModeTests = PASS, minimum 76 tests
UnityCompile = PASS
ScopeValidation = PASS
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY_RESULT.md
```

모든 gate가 PASS일 때만 CHAR03_02를 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR03_03은 LOCKED로 유지하고 자동 시작하지 않는다.
