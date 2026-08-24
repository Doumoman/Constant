# MAP07_11 - Implement Microchunk CSV Export

MAP07_10 PASS/finalize 후 MAP07의 열한 번째 Task만 여는 patch package다. Apply는 Master, Status, `MAP07_11` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 9bf311d95b4a16518d6e8dea296fd7694c30d225a719c394c91c9addc94c5d7b
Previous MAP07_10 Task SHA-256: a21f95a87c1f962fed4672376d55eb740af6fa5d8b0aa8ec286ba782b2f54735
Current MAP07_11 Task SHA-256: 1359b31bd70bd8288f86fb2d994267d480b7130a96a45e25541de1c05ba7e6ca
State after apply: 88 COMPLETE / MAP07_11 CURRENT / 116 LOCKED
```

실행 범위:

- Selected microchunk ID의 detached editor state를 Authoring CSV로 export.
- Exact selected-ID row replacement.
- `microchunk_tile_cells.csv` selected ID exactly 96 rows, including all-`NONE` cells.
- UTF-8 BOM preservation, RFC4180 serialization, schema-primary-key stable sort.
- Side-effect-free plan generation and atomic real-file application path.
- In-memory/temp-fixture tests only for source mutation coverage.

`MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT`는 PASS 전까지 `LOCKED / DO NOT START`다.
