# RUN CHAR02_03

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
10. CHAR02_01/CHAR02_02 PASS REPORT와 source registry
11. current Character runtime/test code와 MovementCourses
12. PASS일 때만 `MCP/08_STATUS_FINALIZE_RULES.md`

Exact gates:

```text
CHAR02_02 Result STATUS: PASS
CHAR02_02 Result SHA-256: 09e033b8d559afbefa7f761f4367c5294e06ab9f44cdd0f153966bf0af5cb192
CHAR02_02 Task SHA-256: e290545cb0ff8a64f2de1e30c1426522a2d9757a18b29c65e703b30c9a115458
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR02_03 Task SHA-256: e99b725df83b4795a4963709c74335580183a821eca48e1dd51fbf734a10270c
Status payload SHA-256: 99fd8db79e06dc0bebc46a5ba39eef5b5e36fa4c4547fdafc61dfb2252d77edd
Master payload SHA-256: d4fe3a80b300202135fab4358a7f2419eb40a0d9da654bec6cfc34175942505e
```

어느 hash나 상태가 다르면 BLOCKED하고 변경하지 않는다. CHAR03_01 이후 Task body는 읽거나 시작하지 않는다.

Required audit gates:

```text
PriorEvidenceAndState = PASS
MovementGrammarCoverage = PASS or explicit FAIL/BLOCKED with evidence
CoyoteDelayedJumpRiskDecision = REQUIRED
CourseFixtureAndDeterminism = PASS
ForbiddenMovementAbsence = PASS
TargetedEditModeTests = PASS, minimum 52 tests
UnityCompile = PASS
ScopeValidation = PASS
REPORT 외 unauthorized changes = 0
```

REPORT 경로:

```text
CharacterDesign/MCP/REPORTS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT_RESULT.md
```

모든 gate가 PASS일 때만 CHAR02_03을 COMPLETE, Current Task를 NONE으로 finalize한다. CHAR03_01은 LOCKED로 유지하고 자동 시작하지 않는다.
