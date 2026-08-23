# MAP07_05 — Implement Object Slot Validation

MAP07_04 PASS/finalize 후 MAP07의 다섯 번째 Task만 여는 patch package다. Apply는 Master, Status, `MAP07_05` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 90bb39103282ad08d031ee710802abdeba0adc4799c754ba73eaede4a2b7ade5
Previous MAP07_04 Task SHA-256: a563b469ebcfe9bea8f7f280398f20aa4464fd2aed9ff5ac2000c60f773eb0a6
Current MAP07_05 Task SHA-256: 141ba64ee4fadee918c69daa94693a89aac21efb10d14f65576c04c4e66515fc
State after apply: 82 COMPLETE / MAP07_05 CURRENT / 122 LOCKED
```

실행 범위:

- Object slot anchor coordinate, category, pool compatibility 검증.
- Required flag, orientation, visible flag 저장/검증.
- Required marker code 존재 및 anchor marker-layer 일치 검증.
- GroundSolid, Breakable, Hazard, Liquid 내부 배치 금지.
- Manhattan forbidden-radius safety check와 slot spacing 검증.
- `allowed_pool_id`는 in-memory validation policy와 category compatibility만 검증하며 spawn/item/prefab 선택은 하지 않음.

`MAP07_06_IMPLEMENT_96_CELL_VALIDATOR`는 PASS 전까지 `LOCKED / DO NOT START`다.
