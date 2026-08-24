# RUN CHAR02_02

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
10. CHAR02_01 PASS REPORT와 source registry
11. current Character runtime/test code와 MovementCourses
12. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR02_01 Result STATUS: PASS
CHAR02_01 Result SHA-256: 7115475798e10b6de07b4ffb1a13695c47dcfe8b004c56cb2e857b3b435d36ad
CHAR02_01 Task SHA-256: 678ed6579dfbd8df99ff00ae841829ea8243c3c477ad62fdc2b865a0dfa0624b
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR02_02 Task SHA-256: e290545cb0ff8a64f2de1e30c1426522a2d9757a18b29c65e703b30c9a115458
Status payload SHA-256: dcb49d185acabd2fc638642c02ff060f5c084dba6da5dc2bb8c830e7aba2281e
Master payload SHA-256: 1e1c4dc067061952e86745bb3ed84a1ddb5c2340e934e6cc2696331caa559549
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR02_03 이후 Task body는 읽거나 시작하지 않는다.

Required Task gates:

```text
ThreeCellGapFailure = PASS
ForbiddenMovementAbsence = PASS
TwoCellRegression = PASS
TargetedEditModeTests = PASS (existing 44 + new 8)
UnityCompile = PASS
ScopeValidation = PASS
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR02_02_VALIDATE_THREE_CELL_FAILURE_AND_FORBIDDEN_MOVEMENT_RESULT.md
```

모든 gate가 PASS일 때만 CHAR02_02를 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR02_03은 LOCKED로 유지하고 자동 시작하지 않는다.
