# RUN CHAR01_01

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
10. CHAR00_03 PASS REPORT와 source registry
11. fixed specs, action schema, movement tuning schema, movement fixture
12. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR00_03 Result STATUS: PASS
CHAR00 EXIT: APPROVED
CHAR01_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH
CHAR00_03 Result SHA-256: c9b1804527c8c381cb8f6e07b0019fe5a5d458340aeb621d6e847d280c75c138
CHAR00_03 Task SHA-256: 05cb7ccc006511adf854126d0c438cb23bf7a53045044f494c55f74664bea342
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR01_01 Task SHA-256: af23f259463041abf62ebc83aeec51e20ab78fbeef5a76f8cfc7ac851e7129e4
Status payload SHA-256: 55cf86be1f8ceb707abf7b9b4980e1459541cd6629f28455d56c90fa2ec5b089
Master payload SHA-256: c0fd733071ebcca72c5c7112a3e9e791caa82e234e7da78417b8b16efa37cec0
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR01_02 이후 Task body는 읽거나 시작하지 않는다.

Required Task gates:

```text
ApprovedAssemblyPlacement = PASS
InputSnapshotContract = PASS
InputBufferContract = PASS
PlayerStateContract = PASS
ForbiddenFeatureAbsence = PASS
TargetedEditModeTests = PASS (12/12 required names)
UnityCompile = PASS
ScopeValidation = PASS
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES_RESULT.md
```

모든 gate가 PASS일 때만 CHAR01_01을 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR01_02는 LOCKED로 유지하고 자동 시작하지 않는다.
