# MAP06_05 — Assign Access Rules and Clues

MAP06_04 PASS/finalize 후 MAP06의 다섯 번째 Task만 여는 patch package다. Apply는 Master, Status, `MAP06_05` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP06_04_ASSIGN_TYPE0_ROUTE_MASKS_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 7cfb055bb6cb1df24206b25a1a5f046936c7fbdf58bd4b307d476ead4f28ed7a
Previous MAP06_04 Task SHA-256: 320870304bc61d7414a10473978ae11472adefd88c6f8cd76bb6f909ac136cea
Current MAP06_05 Task SHA-256: d80cf04261811777b65b6c99ca8b7ae368fc39f4a895d024c6639ada5226c587
State after apply: 72 COMPLETE / MAP06_05 CURRENT / 132 LOCKED
```

실행 범위:

- MAP06_04의 12 Type0 optional regions에 `Basic / Tool / Environment / Explosive / Hidden` access rule을 deterministic하게 배정.
- 각 region에 mandatory 쪽에서 인지 가능한 clue exact 1개를 예약.
- MAP06_06 입력용 tool tier, explosive fuel cost, hidden clue difficulty만 depth별로 계산.
- attachment→mandatory boundary는 base-closed 상태를 유지하며 actual edge signature/socket/generated edge/CSV는 생성하지 않음.
- 신규 Runtime production C# 8개, Runtime EditMode test C# 1개 생성.
- 기존 boundary assertions는 MAP06_05 symbols를 허용하고 MAP06_06+만 금지하도록 필요한 파일만 수정 가능.
- reward tier, return policy, inactive buffer, validator, overlay는 구현하지 않음.

Type4 기준은 유지한다: U+D mandatory, L/R independent, `UD/LUD/RUD/LRUD` all legal. Authoring CSV는 원본이므로 수정하지 않는다. `MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER`는 PASS 전까지 `LOCKED / DO NOT START`다.

