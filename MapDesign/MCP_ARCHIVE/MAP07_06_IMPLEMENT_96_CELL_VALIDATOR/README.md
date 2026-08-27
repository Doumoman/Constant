# MAP07_06 - Implement 96 Cell Validator

MAP07_05 PASS/finalize 후 MAP07의 여섯 번째 Task만 여는 patch package다. Apply는 Master, Status, `MAP07_06` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP07_05_IMPLEMENT_OBJECT_SLOT_VALIDATION_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 4d805c6ff1702e4e8ecea3be7a337584e4e2856b7d5106d51d1e42c31954029c
Previous MAP07_05 Task SHA-256: 141ba64ee4fadee918c69daa94693a89aac21efb10d14f65576c04c4e66515fc
Current MAP07_06 Task SHA-256: 38a601ca63dff23622564cf36b3c02aa2f55849808c69b3a58bf60d2a8d7c6fa
State after apply: 83 COMPLETE / MAP07_06 CURRENT / 121 LOCKED
```

실행 범위:

- Complete microchunk tile data의 exact `12x8 = 96` row coverage 검증.
- `0..11 x 0..7` 좌표 누락, 중복, 범위 초과 검출.
- Empty tile도 explicit `NONE` row로 있어야 한다는 sparse-row 금지 계약 검증.
- Partial/draft policy에서는 missing을 허용할 수 있지만 duplicate/out-of-range는 계속 실패 처리.
- Tile-layer compatibility, socket-edge, object-slot, reachability, CSV import/export는 구현하지 않음.

`MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE`는 PASS 전까지 `LOCKED / DO NOT START`다.
