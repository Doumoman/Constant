# RUN CHAR06_01

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
10. CHAR05_05 PASS/APPROVED REPORT와 source registry
11. current Character runtime/test code
12. MAP public generated map/route contracts는 source registry 경로로만 확인
13. legacy generated-map/player-spawn examples는 read-only reference로만 확인
14. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR05_05 Result STATUS: PASS
CHAR05_05 Result SHA-256: cb7f4d136e6ff09183065754f4a22a1da4deab1311c80c7e205489e7cb0b17a6
CHAR05_EXIT_DECISION: APPROVED
CHAR05_05 Task SHA-256: b740d76985ef294defc53c04d885d57ef64cc833674cbb1313a259d6850531f6
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR06_01 Task SHA-256: b85b6097dbeb1fcef04343e8e8d78010ca2852fe821e5cce162808f28abd58c1
Status payload SHA-256: 82a572f784dcd0dc71d319155f52b71cb462c110f8f958387fa67e8b8b179957
Master payload SHA-256: adff93de3adec2def466ef44436841d49da4c1f1207aa36d3c04746a477c0be8
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR06_02 이후 Task body는 읽거나 시작하지 않는다.

Required gates:

```text
CHAR05ExitVerified = PASS
GeneratedMapStartAndSpawn = PASS
GeneratedRouteTransition = PASS
RouteCapabilityCheck = PASS
IntegrationRequestBatch = PASS
AuthorityAndForbiddenFeatureGuard = PASS
TargetedEditModeTests = PASS, minimum 170 tests
UnityCompile = PASS
ScopeValidation = PASS
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES_RESULT.md
```

모든 gate가 PASS일 때만 CHAR06_01을 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR06_02는 LOCKED로 유지하고 자동 시작하지 않는다.
