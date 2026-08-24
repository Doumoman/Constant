# RUN CHAR00_03

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
10. CHAR00_01/02 PASS REPORT와 source registry
11. fixed specs 8, schemas 4, fixtures 4
12. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR00_02 Result STATUS: PASS
CHAR00_02 Result SHA-256: 87d91f2a9dbede08050a9b34aa05544f40ff8d4bafb48ed59321db00f5471124
CHAR00_02 Task SHA-256: 29db7d180e8675b17858601f8ba6e9e2aeae03059a24c4252b3efb24dc04b51b
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR00_03 Task SHA-256: 05cb7ccc006511adf854126d0c438cb23bf7a53045044f494c55f74664bea342
Status payload SHA-256: 6255c2a76409d53f265699e13e291a5396db2a2423e3d6edeb55bb2a2e8f6a82
Master payload SHA-256: 1ccb808de1ac5be8b87bc8c3949dff2f0cc0e69a6640721c1b854e66daa7d541
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR01_01 이후 Task body는 읽거나 시작하지 않는다.

Required Task gates:

```text
PriorEvidenceAndState = PASS
ContractCrossConsistency = PASS
FixtureAndSchemaCompleteness = PASS (fixture IDs 16/16)
NoPrematureImplementation = PASS
DependencyLedger = PASS
CHAR00ExitDecision = PASS
REPORT 외 changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT_RESULT.md
```

모든 gate가 PASS일 때만 REPORT에 `CHAR00 EXIT: APPROVED`, `CHAR01_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH`를 기록하고 CHAR00_03을 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR01_01은 LOCKED로 유지하고 자동 시작하지 않는다.
