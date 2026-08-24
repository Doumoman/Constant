# CHAR01_01 — Input Snapshot and Player States

CHAR00 EXIT 승인 후 CHAR01_01 구현 task 하나만 여는 patch package다. PATCH APPLY는 Master, Status, CHAR01_01 Task 문서만 설치하고 Assets 구현은 Task execution에서만 수행한다.

기준선:

```text
Prior Result: CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: c9b1804527c8c381cb8f6e07b0019fe5a5d458340aeb621d6e847d280c75c138
Previous CHAR00_03 Task SHA-256: 05cb7ccc006511adf854126d0c438cb23bf7a53045044f494c55f74664bea342
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
Current CHAR01_01 Task SHA-256: af23f259463041abf62ebc83aeec51e20ab78fbeef5a76f8cfc7ac851e7129e4
State after apply: 3 COMPLETE / CHAR01_01 CURRENT / 22 LOCKED
```

Task 실행 범위:

- 캐릭터 첫 활성 runtime/test 배치 확정
- 논리 입력 스냅샷, 입력 버퍼, 입력 lock reason set 구현
- 플레이어 상태와 immutable snapshot 구현
- 지정된 12개 EditMode test case 작성·실행
- inputactions, Rigidbody2D, 충돌/점프 motor, MAP 연동, PlayMode는 제외

`CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR`는 PASS/finalize 후에도 LOCKED다.
