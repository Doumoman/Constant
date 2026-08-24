# CHAR01_03 — Jump, Air Control, and Landing

CHAR01_02 PASS/finalize 후 CHAR01_03 구현 task 하나만 여는 patch package다. PATCH APPLY는 Master, Status, CHAR01_03 Task 문서만 설치하고 Assets 구현은 Task execution에서만 수행한다.

기준선:

```text
Prior Result: CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: bc637e315cd123ea689977ce173fd70f848048bf7a7dcb527e8de2dd53553186
Previous CHAR01_02 Task SHA-256: 448516103d18a2fea2716e08d60929a735e462aa0e9f7774a30d4fb8695127b4
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
Current CHAR01_03 Task SHA-256: 4f28c237637c9ace93e87250240cd61d1c8db9cbb384ed5ea5d038e5bdf9b99d
State after apply: 5 COMPLETE / CHAR01_03 CURRENT / 20 LOCKED
```

Task 실행 범위:

- 점프 버퍼, 코요테 시간, 단일 점프 소비
- 가변 점프 release, rise/fall gravity, max fall speed
- 공중 수평 제어와 착지 전환
- 지정된 CHAR01_03 EditMode test 12개 + 기존 CHAR01_01/02 test 24개 재검증
- 2셀/3셀 코스 검증, MAP 연동, PlayMode는 제외

`CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT`는 PASS/finalize 후에도 LOCKED다.
