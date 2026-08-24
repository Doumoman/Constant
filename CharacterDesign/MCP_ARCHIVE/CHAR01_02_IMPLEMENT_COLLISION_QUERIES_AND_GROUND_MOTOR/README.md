# CHAR01_02 — Collision Queries and Ground Motor

CHAR01_01 PASS/finalize 후 CHAR01_02 구현 task 하나만 여는 patch package다. PATCH APPLY는 Master, Status, CHAR01_02 Task 문서만 설치하고 Assets 구현은 Task execution에서만 수행한다.

기준선:

```text
Prior Result: CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 092ddca26e29c7b37062232a1d7e29139865539c3eac09dcf8aa85b6597506e6
Previous CHAR01_01 Task SHA-256: af23f259463041abf62ebc83aeec51e20ab78fbeef5a76f8cfc7ac851e7129e4
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
Current CHAR01_02 Task SHA-256: 448516103d18a2fea2716e08d60929a735e462aa0e9f7774a30d4fb8695127b4
State after apply: 4 COMPLETE / CHAR01_02 CURRENT / 21 LOCKED
```

Task 실행 범위:

- 캐릭터 충돌 질의 abstraction과 Unity Physics2D adapter
- locked capsule baseline 기반 ground probe
- 지상 걷기·달리기·가속·감속·방향 전환 motor
- 지정된 CHAR01_02 EditMode test 12개 + 기존 CHAR01_01 test 12개 재검증
- 점프, 중력, 공중 제어, 착지, MAP 연동, PlayMode는 제외

`CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING`는 PASS/finalize 후에도 LOCKED다.
