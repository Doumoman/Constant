# RUN CHAR02_01

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
10. CHAR01_04 PASS REPORT와 source registry
11. current Character runtime/test code와 MOVEMENT_COURSE_SPEC
12. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR01_04 Result STATUS: PASS
CHAR01 EXIT: APPROVED
CHAR02_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH
CHAR01_04 Result SHA-256: e9abb9a337c7621b74e376f58193850c274a5f2b3937eec9c17495361599d15e
CHAR01_04 Task SHA-256: ce1f06036b4b75d44af17eb30ede14f69d148b9c097ef6dc691fd8fa1e4f2837
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR02_01 Task SHA-256: 678ed6579dfbd8df99ff00ae841829ea8243c3c477ad62fdc2b865a0dfa0624b
Status payload SHA-256: 723221a568fdd7299f8ffaf843229bb9ec8a2e6f7fbd3ef4ff785189582b336d
Master payload SHA-256: a7c5eee8955badc15ac375f6b19a144f82f5583dcc778243c24de92f40a4e68d
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR02_02 이후 Task body는 읽거나 시작하지 않는다.

Required Task gates:

```text
TwoCellHeightCourse = PASS
TwoCellGapCourse = PASS
MovementCoreUsage = PASS
TargetedEditModeTests = PASS (existing 36 + new 8)
UnityCompile = PASS
ScopeValidation = PASS
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES_RESULT.md
```

모든 gate가 PASS일 때만 CHAR02_01을 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR02_02는 LOCKED로 유지하고 자동 시작하지 않는다.
