# RUN CHAR04_04

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
10. CHAR04_01/02/03 PASS REPORT와 source registry
11. current Character runtime/test code
12. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR04_03 Result STATUS: PASS
CHAR04_03 Result SHA-256: 14752158017446a9f49ff4c7088fdeb043b6886b9bfa48d60f378fe5ba85c1ab
CHAR04_03 Task SHA-256: 8f45c9259196844d8d35d7b94bce3c0ba0c9f0904e77943663fac3034fb5fe8a
CHAR04_02 Result SHA-256: e68259585ed2cfd4ec4baf01cccb732dc073f3a372551725e1c8c185e4d0366f
CHAR04_01 Result SHA-256: 115949eb70478f68195b22f9ecfa6d2a2cc73872c69ba53aaf7ff772da26a247
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR04_04 Task SHA-256: d87add58019b97ea23e2156bf309d5d651932cf9f0e03279283c7f120166e348
Status payload SHA-256: 968a3ec4af0c419e9eac1a5f32620995a370ede4579e3baf45a04e4358fa6ea0
Master payload SHA-256: 116a44688e60cdbd71ce375bcea638766af9d288283776ddedadd1f79456a824
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR05 이후 Task body는 읽거나 시작하지 않는다.

Required gates:

```text
CHAR04PhaseLedger = PASS
InteractionContractAudit = PASS
CombatContractAudit = PASS
ImpactContractAudit = PASS
ForbiddenFeatureGuard = PASS
DependencyDirection = PASS
TargetedEditModeTests = PASS, minimum 110 tests
UnityCompile = PASS
ScopeValidation = PASS, report-only
CHAR04_EXIT_DECISION = APPROVED
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT_RESULT.md
```

모든 gate가 PASS일 때만 CHAR04_04를 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR05_01은 LOCKED로 유지하고 자동 시작하지 않는다.
