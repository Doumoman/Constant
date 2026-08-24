# RUN CHAR04_02

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
10. CHAR04_01 PASS REPORT와 source registry
11. current Character runtime/test code
12. legacy contact/combat examples는 read-only reference로만 확인
13. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR04_01 Result STATUS: PASS
CHAR04_01 Result SHA-256: 115949eb70478f68195b22f9ecfa6d2a2cc73872c69ba53aaf7ff772da26a247
CHAR04_01 Task SHA-256: bc3587cd7e6818eea2cec12f9135244ef7bb27c6a5dcbbdcd79e9a5bec252845
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR04_02 Task SHA-256: da237cb82eda9f807656d4cd7efd1226577b9d6ce704dd745649bb43a6e220bf
Status payload SHA-256: eb3f6586e0baf4280d3359c2aec12b6159d07dad9665445141654a317c6b1e2f
Master payload SHA-256: 0f5f833141c353369df16fc338329fa17c86c2534cad0dc367b7b49629e09dc5
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR04_03 이후 Task body는 읽거나 시작하지 않는다.

Required gates:

```text
CHAR04_01CarryContractVerified = PASS
ContactClassification = PASS
StompAndRebound = PASS
EnemyStunRemovalFlow = PASS
PlayerContactDamage = PASS
StunnedEnemyCarryBridge = PASS
ForbiddenFeatureGuard = PASS
TargetedEditModeTests = PASS, minimum 100 tests
UnityCompile = PASS
ScopeValidation = PASS
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE_RESULT.md
```

모든 gate가 PASS일 때만 CHAR04_02를 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR04_03은 LOCKED로 유지하고 자동 시작하지 않는다.
