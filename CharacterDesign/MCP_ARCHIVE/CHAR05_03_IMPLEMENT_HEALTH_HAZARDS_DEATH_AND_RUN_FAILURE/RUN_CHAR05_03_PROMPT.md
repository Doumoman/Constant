# RUN CHAR05_03

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
10. CHAR05_02 PASS REPORT와 source registry
11. current Character runtime/test code
12. legacy health/hazard/death examples는 read-only reference로만 확인
13. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR05_02 Result STATUS: PASS
CHAR05_02 Result SHA-256: 940e5cf9909cc55a6562704c530ee7abba2d9638ac52627d9b0146922cb98fef
CHAR05_02 Task SHA-256: 0ba1e7c1b3b41e76419fb477a071dfd25226de02f7808ea4030b7c64c8198d8b
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR05_03 Task SHA-256: 4d7750096baf950b4c94dc72a76d0c21be64847b34580960db7fa8fa311e52f0
Status payload SHA-256: c4bd24fa18a0bad9c252d0f6c31928f4abba5a039defb92f8c1d355c97652aab
Master payload SHA-256: 5fea8d8c68d295f89353d7ad2820b79013068da1f06bb1ac67ceb4c02eced50a
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR05_04 이후 Task body는 읽거나 시작하지 않는다.

Required gates:

```text
CHAR05_02RopeContractVerified = PASS
HealthStateAndDamage = PASS
UnifiedDamageRequests = PASS
HazardCandidates = PASS
DeathRequest = PASS
RunFailureAndReturnRequest = PASS
AuthorityAndForbiddenFeatureGuard = PASS
TargetedEditModeTests = PASS, minimum 146 tests
UnityCompile = PASS
ScopeValidation = PASS
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE_RESULT.md
```

모든 gate가 PASS일 때만 CHAR05_03을 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR05_04는 LOCKED로 유지하고 자동 시작하지 않는다.
