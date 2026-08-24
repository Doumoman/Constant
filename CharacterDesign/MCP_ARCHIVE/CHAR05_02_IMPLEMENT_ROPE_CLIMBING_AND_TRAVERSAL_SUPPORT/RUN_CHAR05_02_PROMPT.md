# RUN CHAR05_02

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
10. CHAR05_01 PASS REPORT와 source registry
11. current Character runtime/test code
12. MAP public contract는 source registry 경로로만 확인
13. legacy rope/climb examples는 read-only reference로만 확인
14. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR05_01 Result STATUS: PASS
CHAR05_01 Result SHA-256: 1c5036404d957cc5ca534d4c0ec89e77995c3d6adfa66306b73915bc42005e7f
CHAR05_01 Task SHA-256: a12be0d748a606f3c08c9267199bf2e7b74f92002e4ce316c73c63970c406d12
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR05_02 Task SHA-256: 0ba1e7c1b3b41e76419fb477a071dfd25226de02f7808ea4030b7c64c8198d8b
Status payload SHA-256: 500e6ea26981ff3abc130c10f5ad71eba7aff9be98e585cbd62e9782e3645378
Master payload SHA-256: 26598cb875f8589348bd61656e97f8a57749d0d7802b1b84e6ec91d62869e6ba
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR05_03 이후 Task body는 읽거나 시작하지 않는다.

Required gates:

```text
CHAR05_01BombContractVerified = PASS
RopePlacement = PASS
RopeSegmentGeneration = PASS
RopeClimbTraversal = PASS
RopeBoundaryRules = PASS
AuthorityAndForbiddenFeatureGuard = PASS
TargetedEditModeTests = PASS, minimum 134 tests
UnityCompile = PASS
ScopeValidation = PASS
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT_RESULT.md
```

모든 gate가 PASS일 때만 CHAR05_02를 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR05_03은 LOCKED로 유지하고 자동 시작하지 않는다.
