# MAP07_09 - Create Socket and Slot Editor

MAP07_08 PASS/finalize 후 MAP07의 아홉 번째 Task만 여는 patch package다. Apply는 Master, Status, `MAP07_09` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 3f0a2ec3c3f8668de33f180521a872a58a7cc7cb3ea11cb451dd5fcb640200d9
Previous MAP07_08 Task SHA-256: 6d3b211b593743d9aebf6ba4f0c4fc9ef720d85139e9fe1e687231014ee00f29
Current MAP07_09 Task SHA-256: 5e870b792acdaff3ffb12058919f8973cd0fa50dcfd505b662c323f47a6f1a87
State after apply: 86 COMPLETE / MAP07_09 CURRENT / 118 LOCKED
```

실행 범위:

- Editor-only socket list, band list, and edge-signature selection UI.
- Editor-only object slot anchor/category/pool/orientation UI.
- In-memory projection into existing runtime definition types.
- Validation feedback from existing socket-edge and object-slot validators only.
- No CSV import/export, preview/report, starter round-trip, sector assembly, or world traversal.

`MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT`는 PASS 전까지 `LOCKED / DO NOT START`다.
