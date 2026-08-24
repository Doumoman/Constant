# RUN CHAR04_03

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
10. CHAR04_02 PASS REPORT와 source registry
11. current Character runtime/test code
12. legacy throw/impact examples는 read-only reference로만 확인
13. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR04_02 Result STATUS: PASS
CHAR04_02 Result SHA-256: e68259585ed2cfd4ec4baf01cccb732dc073f3a372551725e1c8c185e4d0366f
CHAR04_02 Task SHA-256: da237cb82eda9f807656d4cd7efd1226577b9d6ce704dd745649bb43a6e220bf
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR04_03 Task SHA-256: 8f45c9259196844d8d35d7b94bce3c0ba0c9f0904e77943663fac3034fb5fe8a
Status payload SHA-256: 8dff146b9e2f49c42d2e80d566cea94dc87573326f376dd4e61b79753cf200ec
Master payload SHA-256: bfa20af08fb46c876b09fc77a521476a60f1edcfa158860c2d4784ee3a9776b9
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR04_04 이후 Task body는 읽거나 시작하지 않는다.

Required gates:

```text
CHAR04_02CombatContractVerified = PASS
ImpactSourceTarget = PASS
ThrownObjectEnemyImpact = PASS
OwnerGraceImpactSuppression = PASS
SolidWorldImpact = PASS
NoBasicAttackGuard = PASS
TargetedEditModeTests = PASS, minimum 110 tests
UnityCompile = PASS
ScopeValidation = PASS
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK_RESULT.md
```

모든 gate가 PASS일 때만 CHAR04_03을 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR04_04는 LOCKED로 유지하고 자동 시작하지 않는다.
