# MAP07_08 - Create Microchunk Authoring Grid

MAP07_07 PASS/finalize 후 MAP07의 여덟 번째 Task만 여는 patch package다. Apply는 Master, Status, `MAP07_08` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: afaf3f058c34457d26491b15c06858ba1c1c7355cf14d5902d65f66a43a1fa19
Previous MAP07_07 Task SHA-256: 0d9ec87691cf31db249b2fed7b411ea6b69a1d8c456469672c96999145add103
Current MAP07_08 Task SHA-256: 6d3b211b593743d9aebf6ba4f0c4fc9ef720d85139e9fe1e687231014ee00f29
State after apply: 85 COMPLETE / MAP07_08 CURRENT / 119 LOCKED
```

실행 범위:

- Editor-only 12x8 fixed microchunk grid.
- Exact 8 layer painting state and palette.
- Runtime projection to 96 `MicrochunkTileCell`/coverage records.
- Inline feedback from existing tile-layer and 96-cell validators only.
- New Editor production folder/test folder `.meta` creation is explicitly allowed.

`MAP07_09_CREATE_SOCKET_AND_SLOT_EDITOR`는 PASS 전까지 `LOCKED / DO NOT START`다.
