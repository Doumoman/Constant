# RUN CHAR01_02

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
10. CHAR01_01 PASS REPORT와 source registry
11. current Character runtime/test code와 movement fixed spec/schema
12. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR01_01 Result STATUS: PASS
CHAR01_01 Result SHA-256: 092ddca26e29c7b37062232a1d7e29139865539c3eac09dcf8aa85b6597506e6
CHAR01_01 Task SHA-256: af23f259463041abf62ebc83aeec51e20ab78fbeef5a76f8cfc7ac851e7129e4
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR01_02 Task SHA-256: 448516103d18a2fea2716e08d60929a735e462aa0e9f7774a30d4fb8695127b4
Status payload SHA-256: 37db058a1601cdc974c6bd7f970021af4f6af891710a2e917731960dd6b99250
Master payload SHA-256: 29c3bb99a6ad161201e7d52e0aa4c830945dea67e27de7503ead37fbe1331ecc
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR01_03 이후 Task body는 읽거나 시작하지 않는다.

Required Task gates:

```text
CollisionQueryContract = PASS
GroundProbeContract = PASS
GroundMotorContract = PASS
ForbiddenFeatureAbsence = PASS
TargetedEditModeTests = PASS (CHAR01_02 12/12 + CHAR01_01 12/12)
UnityCompile = PASS
ScopeValidation = PASS
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR_RESULT.md
```

모든 gate가 PASS일 때만 CHAR01_02를 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR01_03은 LOCKED로 유지하고 자동 시작하지 않는다.
