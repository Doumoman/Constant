# MAP07_02 — Implement Tile Layer Rules

MAP07_01 PASS/finalize 후 MAP07의 두 번째 Task만 여는 patch package다. Apply는 Master, Status, `MAP07_02` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: b11e740b808effe5a528a68497527edd0ab92fcc8c1a823dd6baa0d39363f474
Previous MAP07_01 Task SHA-256: 912028220492f7e9dff40db93711dd590dcd73531131d133cd0270c4862d368c
Current MAP07_02 Task SHA-256: 0b69d8f46654bd2af5e441d603210a1889351cff478b688a23b6b87c697ea9c7
State after apply: 79 COMPLETE / MAP07_02 CURRENT / 125 LOCKED
```

실행 범위:

- `MicrochunkTileCell`의 eight logical layers에 대한 deterministic compatibility rule matrix 구현.
- Cell/definition-level rule result와 violation report 생성.
- Decoration overlay, Marker allowed pairs, forbidden blocking/liquid/hazard combinations 검증.
- `MicrochunkTileLayerRulesTests`와 phase-boundary forbidden-symbol scan 갱신.
- Transform, socket-edge validation, object-slot semantic validation, 96-cell validator, reachability, editor UI, CSV import/export는 구현하지 않음.

`MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS`는 PASS 전까지 `LOCKED / DO NOT START`다.
