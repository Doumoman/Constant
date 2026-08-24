# MAP07_10 - Implement Microchunk CSV Import

MAP07_09 PASS/finalize 후 MAP07의 열 번째 Task만 여는 patch package다. Apply는 Master, Status, `MAP07_10` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP07_09_CREATE_SOCKET_AND_SLOT_EDITOR_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 7bc550e92359f4f24c642b24000be1e1a8198fdeb014ce1685555bf5f83a0340
Previous MAP07_09 Task SHA-256: 5e870b792acdaff3ffb12058919f8973cd0fa50dcfd505b662c323f47a6f1a87
Current MAP07_10 Task SHA-256: a21f95a87c1f962fed4672376d55eb740af6fa5d8b0aa8ec286ba782b2f54735
State after apply: 87 COMPLETE / MAP07_10 CURRENT / 117 LOCKED
```

실행 범위:

- Selected microchunk ID의 Authoring CSV import.
- Catalog/tile cells/sockets/bands/object slots/variants read-only parsing.
- In-memory hydration into existing grid and socket/slot editor state.
- Existing validator feedback only.
- No CSV export, row replacement, source mutation, preview/report, or generated CSV.

`MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT`는 PASS 전까지 `LOCKED / DO NOT START`다.
