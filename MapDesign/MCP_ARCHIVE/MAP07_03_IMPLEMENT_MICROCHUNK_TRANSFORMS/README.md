# MAP07_03 — Implement Microchunk Transforms

MAP07_02 PASS/finalize 후 MAP07의 세 번째 Task만 여는 patch package다. Apply는 Master, Status, `MAP07_03` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP07_02_IMPLEMENT_TILE_LAYER_RULES_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 98240add84d955ffdc50c3e22e18eb3a0255d9a1d397e9d6c2039e2488dafc4e
Previous MAP07_02 Task SHA-256: c9cb155bdb0b9f2d047b8305c35f32392d691988f612bc107849d0a9f3292edb
Current MAP07_03 Task SHA-256: 82434805780000e3695cbdda45d5888c4234ba617bdc5bcded843643b4c7aac8
State after apply: 80 COMPLETE / MAP07_03 CURRENT / 124 LOCKED
```

실행 범위:

- `MicrochunkDefinition`의 tile cells, sockets, object slots에 `R0`, `MIRROR_X`, `MIRROR_Y`, `R180` 변환 구현.
- 12x8 좌표, socket side, object slot orientation, marker/cell 이동의 결정적 projection 검증.
- `R90`/`R270` 및 arbitrary 90-degree rotation 거부.
- Direction-facing tile code와 socket band ID는 추측 변환하지 않고 optional remapper가 있을 때만 재매핑.
- `MicrochunkTransformerTests`와 phase-boundary forbidden-symbol scan 갱신.

`MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION`은 PASS 전까지 `LOCKED / DO NOT START`다.
