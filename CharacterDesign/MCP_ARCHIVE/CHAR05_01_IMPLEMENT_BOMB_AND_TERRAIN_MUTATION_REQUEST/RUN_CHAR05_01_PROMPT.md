# RUN CHAR05_01

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
10. CHAR04_04 PASS REPORT와 source registry
11. current Character runtime/test code
12. MAP public contract는 source registry 경로로만 확인
13. legacy bomb/explosion examples는 read-only reference로만 확인
14. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR04_04 Result STATUS: PASS
CHAR04_04 Result SHA-256: fc0fde8fc75d170f6eafd8436f5e21fb49b2b2b2990fba1bcb75c47ba5b38ab2
CHAR04_04 Task SHA-256: d87add58019b97ea23e2156bf309d5d651932cf9f0e03279283c7f120166e348
CHAR04_EXIT_DECISION: APPROVED
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR05_01 Task SHA-256: a12be0d748a606f3c08c9267199bf2e7b74f92002e4ce316c73c63970c406d12
Status payload SHA-256: ea7e2bcdd18b1b8436ab0589378769e06b01e9bf356389c2ee80bb3cb3074aa5
Master payload SHA-256: 30b28eddb7be641a12f150e839fdec4b005e0968780d17dc716bca3888ece6a8
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR05_02 이후 Task body는 읽거나 시작하지 않는다.

Required gates:

```text
CHAR04ExitVerified = PASS
BombPlacement = PASS
BombFuseAndExplosion = PASS
TerrainMutationRequest = PASS
ExplosionDamageCandidates = PASS
AuthorityAndForbiddenFeatureGuard = PASS
TargetedEditModeTests = PASS, minimum 122 tests
UnityCompile = PASS
ScopeValidation = PASS
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST_RESULT.md
```

모든 gate가 PASS일 때만 CHAR05_01을 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR05_02는 LOCKED로 유지하고 자동 시작하지 않는다.
