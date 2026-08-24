# RUN CHAR00_02

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
10. CHAR00_01 PASS REPORT와 source registry
11. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR00_01 Result STATUS: PASS
CHAR00_01 Result SHA-256: 1bc1a931d43030561014c8cdf49c4609ac635bfd57e27d568ec975abefcef6c0
CHAR00_01 Task SHA-256: 08b8141effaf9c66b0cec28d3e8bfba21023fee3f46800062d3ff70ff640f0f8
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR00_02 Task SHA-256: 29db7d180e8675b17858601f8ba6e9e2aeae03059a24c4252b3efb24dc04b51b
Status payload SHA-256: d7999088c4dcf514433ed25496e992d83f5b0e03b2cf8bc4ba9d41dcf72f624d
Master payload SHA-256: 7747d7551410e5a1714a364a22ff32835ef68f082d560717f09fcc1a32d0d032
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR00_03 이후 Task body는 읽거나 시작하지 않는다.

Required Task gates:

```text
ContractsLocked = PASS
FixturesLocked = PASS (required fixture IDs 16/16)
NoRuntimeMutation = PASS
ReportExact = PASS
Assets/Packages/ProjectSettings/MapDesign/C#/inputactions/asmdef/Scene/Prefab changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES_RESULT.md
```

모든 Task gate와 REPORT가 PASS일 때만 CHAR00_02를 COMPLETE, Current Task를 NONE으로 finalize한다. `CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT`는 LOCKED로 유지하고 자동 시작하지 않는다.
