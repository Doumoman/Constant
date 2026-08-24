# RUN CHAR01_03

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
10. CHAR01_02 PASS REPORT와 source registry
11. current Character runtime/test code와 movement fixed spec/schema
12. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR01_02 Result STATUS: PASS
CHAR01_02 Result SHA-256: bc637e315cd123ea689977ce173fd70f848048bf7a7dcb527e8de2dd53553186
CHAR01_02 Task SHA-256: 448516103d18a2fea2716e08d60929a735e462aa0e9f7774a30d4fb8695127b4
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR01_03 Task SHA-256: 4f28c237637c9ace93e87250240cd61d1c8db9cbb384ed5ea5d038e5bdf9b99d
Status payload SHA-256: 5cfed347b7554e0b83103602f2f686c66f5e3ac505cb3b92a0286693c3313136
Master payload SHA-256: 59fc4f08176fc6654b5facd57087c7e105e96028ca9d57d7bf6688bf0f04e501
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR01_04 이후 Task body는 읽거나 시작하지 않는다.

Required Task gates:

```text
JumpContract = PASS
GravityAndAirControlContract = PASS
LandingContract = PASS
ForbiddenFeatureAbsence = PASS
TargetedEditModeTests = PASS (CHAR01_03 12/12 + CHAR01_01/02 24/24)
UnityCompile = PASS
ScopeValidation = PASS
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING_RESULT.md
```

모든 gate가 PASS일 때만 CHAR01_03을 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR01_04는 LOCKED로 유지하고 자동 시작하지 않는다.
