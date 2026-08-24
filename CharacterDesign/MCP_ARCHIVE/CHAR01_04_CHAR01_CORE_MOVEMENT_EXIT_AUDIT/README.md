# CHAR01_04 — CHAR01 Core Movement Exit Audit

CHAR01_03 PASS/finalize 후 CHAR01_04 종료 감사 task 하나만 여는 patch package다. PATCH APPLY는 Master, Status, CHAR01_04 Task 문서만 설치하고 Assets 구현은 수행하지 않는다.

기준선:

```text
Prior Result: CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 373fb206c50790fc99add891783f99bc969a67273da26e6dbd906ea108cad5d2
Previous CHAR01_03 Task SHA-256: 4f28c237637c9ace93e87250240cd61d1c8db9cbb384ed5ea5d038e5bdf9b99d
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
Current CHAR01_04 Task SHA-256: ce1f06036b4b75d44af17eb30ede14f69d148b9c097ef6dc691fd8fa1e4f2837
State after apply: 6 COMPLETE / CHAR01_04 CURRENT / 19 LOCKED
```

Task 실행 범위:

- CHAR01_01~03 PASS 증빙과 상태 체인 감사
- Character runtime/test assembly와 namespace 경계 감사
- 핵심 이동 구현 커버리지와 금지 기능 부재 감사
- `Game.Character.Tests.EditMode` 전체 36개 회귀 실행
- `CHAR01 EXIT`와 `CHAR02_01 ENTRY` 판정
- REPORT 외 파일 변경 0

`CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES`는 PASS/finalize 후에도 LOCKED다.
