# MAP07_01 — Implement Microchunk Definition

MAP06_10 PASS/finalize 후 MAP07의 첫 Task만 여는 patch package다. Apply는 Master, Status, `MAP07_01` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 690a7cef9dbf1d22416e38b3675d76b0ef758062de2425e8e4841381f0d9bdeb
Previous MAP06_10 Task SHA-256: 623da5aaf2f8c72dd830fb5f859c4b05a631a93b7f4fa2a3aa67adc823f95cdb
Current MAP07_01 Task SHA-256: 912028220492f7e9dff40db93711dd590dcd73531131d133cd0270c4862d368c
State after apply: 78 COMPLETE / MAP07_01 CURRENT / 126 LOCKED
```

실행 범위:

- 12x8 microchunk immutable runtime definition model 생성.
- `MicrochunkId`, local coordinate, constants, enums, tile cell, socket definition, object slot definition, aggregate definition 구현.
- complete definition은 96 unique cells를 요구하고 row-major ordering을 보장.
- `MicrochunkDefinitionTests`와 phase-boundary forbidden-symbol scan 갱신.
- CSV import/export, editor window, transforms, tile layer collision rules, socket edge validation, object slot validation, reachability probe, preview/report는 구현하지 않음.

MAP05 Type4 U+D mandatory, L/R independent, `UD/LUD/RUD/LRUD` all legal 규칙과 MAP06 phase exit source-chain을 유지한다. `MAP07_02_IMPLEMENT_TILE_LAYER_RULES`는 PASS 전까지 `LOCKED / DO NOT START`다.
