# RUN CHAR04_01

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
10. CHAR03_03 PASS REPORT와 source registry
11. current Character runtime/test code
12. legacy carry examples는 read-only reference로만 확인
13. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR03_03 Result STATUS: PASS
CHAR03_03 Result SHA-256: 28e83c356e53683370bc15a787a8f80700ea3fa3052523df6c4b09f9d4812f52
CHAR03_03 Task SHA-256: 644919d9843a92333a4ba7fb069ffd07f3e26ac1c8ffca32fdfc05620c3a690e
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR04_01 Task SHA-256: bc3587cd7e6818eea2cec12f9135244ef7bb27c6a5dcbbdcd79e9a5bec252845
Status payload SHA-256: 02502baabb087f154b3a7dea508054a1d6665ec6774182082360ea2420c60404
Master payload SHA-256: acd98eef12699034c66eacd80215b38dde5aa5348483ef92066e96627e7c7646
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR04_02 이후 Task body는 읽거나 시작하지 않는다.

Required gates:

```text
CHAR03ExitApproved = PASS
CarryCandidateQuery = PASS
SingleCarrySlot = PASS
SafeDrop = PASS
DirectionalThrow = PASS
OwnerCollisionGrace = PASS
ForbiddenFeatureGuard = PASS
TargetedEditModeTests = PASS, minimum 88 tests
UnityCompile = PASS
ScopeValidation = PASS
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW_RESULT.md
```

모든 gate가 PASS일 때만 CHAR04_01을 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR04_02는 LOCKED로 유지하고 자동 시작하지 않는다.
