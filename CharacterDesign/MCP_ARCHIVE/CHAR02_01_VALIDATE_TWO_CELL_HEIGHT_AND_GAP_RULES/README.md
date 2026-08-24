# CHAR02_01 — Validate Two-Cell Height and Gap Rules

CHAR01_04 PASS/finalize 후 CHAR02_01 이동 문법 검증 task 하나만 여는 patch package다. PATCH APPLY는 Master, Status, CHAR02_01 Task 문서만 설치하고 Assets 구현은 Task execution에서만 수행한다.

기준선:

```text
Prior Result: CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: e9abb9a337c7621b74e376f58193850c274a5f2b3937eec9c17495361599d15e
Previous CHAR01_04 Task SHA-256: ce1f06036b4b75d44af17eb30ede14f69d148b9c097ef6dc691fd8fa1e4f2837
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
Current CHAR02_01 Task SHA-256: 678ed6579dfbd8df99ff00ae841829ea8243c3c477ad62fdc2b865a0dfa0624b
State after apply: 7 COMPLETE / CHAR02_01 CURRENT / 18 LOCKED
```

Task 실행 범위:

- test-only MovementCourses 시뮬레이터 추가
- 기본 점프로 2셀 높이 발판 도달 검증
- 달리기 기반 이동으로 동일 높이 2셀 틈 통과 검증
- 기존 CHAR01 36개 + 신규 CHAR02_01 8개 EditMode 테스트 실행
- 3셀 실패와 금지 이동 검증은 제외

`CHAR02_02_VALIDATE_THREE_CELL_FAILURE_AND_FORBIDDEN_MOVEMENT`는 PASS/finalize 후에도 LOCKED다.
