# MAP06_07 — Implement Return Policy

MAP06_06 PASS/finalize 후 MAP06의 일곱 번째 Task만 여는 patch package다. Apply는 Master, Status, `MAP06_07` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 0acfcd73b6485e99a56dd4d44bff50f871548e266ed003607466961632ec449c
Previous MAP06_06 Task SHA-256: 8c8dd6a780b334edf7fb8c1276c1cc5d64332bf26f8c5ab9b69e9dabcb22a542
Current MAP06_07 Task SHA-256: 2ab50e5c150bc833395cd9e5f8acb017e8685d90f0b63d5cab394cf0e33b4956
State after apply: 74 COMPLETE / MAP06_07 CURRENT / 130 LOCKED
```

실행 범위:

- Type0/access/reward source-chain을 검증하고 reciprocal internal BaseEdge로 모든 39 cells의 attachment 복귀 가능성을 증명.
- 각 region의 가장 깊은 canonical cell에서 attachment까지 deterministic shortest witness를 생성.
- 기존 `OptionalReturnPolicy.BacktrackToAttachment`를 12개 region에 기록.
- 같은 opened/discovered optional attachment를 역방향으로 사용하되 base mask는 closed 유지.
- 신규 Runtime production C# 6개, Runtime EditMode test C# 1개 생성.
- existing boundary assertions는 MAP06_07 symbols를 허용하고 MAP06_08+만 금지하도록 필요한 파일만 수정 가능.
- synthetic ReturnGate/SafeExit/device/socket/edge/recipe와 inactive/validator/overlay/generated CSV는 구현하지 않음.

Type4 U+D mandatory, L/R independent, `UD/LUD/RUD/LRUD` all legal 규칙과 Authoring CSV 불변 조건을 유지한다. `MAP06_08_ASSIGN_INACTIVE_BUFFERS`는 PASS 전까지 `LOCKED / DO NOT START`다.

