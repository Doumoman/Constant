# CHAR02_02 — Validate Three-Cell Failure and Forbidden Movement

CHAR02_01 PASS/finalize 후 CHAR02_02 이동 문법 검증 task 하나만 여는 patch package다. PATCH APPLY는 Master, Status, CHAR02_02 Task 문서만 설치하고 Assets 구현은 Task execution에서만 수행한다.

기준선:

```text
Prior Result: CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 7115475798e10b6de07b4ffb1a13695c47dcfe8b004c56cb2e857b3b435d36ad
Previous CHAR02_01 Task SHA-256: 678ed6579dfbd8df99ff00ae841829ea8243c3c477ad62fdc2b865a0dfa0624b
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
Current CHAR02_02 Task SHA-256: e290545cb0ff8a64f2de1e30c1426522a2d9757a18b29c65e703b30c9a115458
State after apply: 8 COMPLETE / CHAR02_02 CURRENT / 17 LOCKED
```

Task 실행 범위:

- 동일 높이 3셀 틈 기본 이동 실패 검증
- wall jump / dash / double jump 부재 검증
- basic attack / melee / shoot 부재 검증
- 기존 44개 + 신규 CHAR02_02 8개 EditMode 테스트 실행
- Runtime, asmdef, inputactions, Scene, Prefab 변경 0

`CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT`는 PASS/finalize 후에도 LOCKED다.
