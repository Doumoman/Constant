# MAP07_07 - Implement Microchunk Reachability Probe

MAP07_06 PASS/finalize 후 MAP07의 일곱 번째 Task만 여는 patch package다. Apply는 Master, Status, `MAP07_07` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP07_06_IMPLEMENT_96_CELL_VALIDATOR_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 81681d92aac6bff244dc7f655014c89cabb43baa178b3355fe701c6046b1a6e0
Previous MAP07_06 Task SHA-256: 38a601ca63dff23622564cf36b3c02aa2f55849808c69b3a58bf60d2a8d7c6fa
Current MAP07_07 Task SHA-256: 0d9ec87691cf31db249b2fed7b411ea6b69a1d8c456469672c96999145add103
State after apply: 84 COMPLETE / MAP07_07 CURRENT / 120 LOCKED
```

실행 범위:

- Complete 12x8 microchunk의 local traversal graph 생성.
- Mandatory no-tool socket entry 후보를 side/band에서 결정.
- 모든 unordered mandatory socket pair에 대해 deterministic shortest-path witness 검증.
- Blocking layer는 GroundSolid, Breakable, Hazard, Liquid.
- OneWay, DecorationBack, DecorationFront, Marker, `NONE`은 blocker가 아님.
- Authoring UI, CSV import/export, preview/report, sector/world traversal은 구현하지 않음.

`MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID`는 PASS 전까지 `LOCKED / DO NOT START`다.
