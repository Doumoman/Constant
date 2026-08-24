# RUN CHAR02_03 REPAIR

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
10. CHAR02_01/CHAR02_02 PASS REPORT와 CHAR02_03 FAIL REPORT
11. source registry
12. current Character movement runtime/test code와 MovementCourses
13. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
Current Task before patch: TASKS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT.md
CHAR02_03 Previous Result STATUS: FAIL
CHAR02_03 Previous Result SHA-256: e5fac10bce6791006c2549134834b8d518d0f9aa1d29d276595ce87203208043
CHAR02_03 Previous Task SHA-256: e99b725df83b4795a4963709c74335580183a821eca48e1dd51fbf734a10270c
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
Revised CHAR02_03 Task SHA-256: 6c4b7f0a9e047db07d3c3c1b667f6b74e619ddddef7d1c1bafa889da52ad2250
Status payload SHA-256: eee6f0e262e3ca2635d12b3d17ab0928e4cc410e72b2486ce865257f31511819
Master payload SHA-256: 955e7719408b3b649bc2428fd2955c018ee2e50b9d0b0ed3dcc86acf09e1ca73
```

Required gates:

```text
ReproduceCoyoteThreeCellFailureBeforeRepair = PASS
MovementRepairChangeControl = PASS
TwoCellHeightRegression = PASS
TwoCellGapRegression = PASS
ThreeCellNormalAndCoyoteFailure = PASS
ForbiddenMovementAbsence = PASS
TargetedEditModeTests = PASS, minimum 57 tests
UnityCompile = PASS
ScopeValidation = PASS
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT_RESULT.md
```

모든 gate가 PASS일 때만 CHAR02_03을 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR03_01은 LOCKED로 유지하고 자동 시작하지 않는다.
