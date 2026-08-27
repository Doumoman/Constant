# MAP07_12 - Create Microchunk Preview And Report

MAP07_11 PASS/finalize 후 MAP07의 열두 번째 Task만 여는 patch package다. Apply는 Master, Status, `MAP07_12` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 340cbed5424208ebeef144028c1806ea6a9039e8a6c14a5f39a824b042b062c6
Previous MAP07_11 Task SHA-256: 1359b31bd70bd8288f86fb2d994267d480b7130a96a45e25541de1c05ba7e6ca
Current MAP07_12 Task SHA-256: 73544122f13653fa3762e87fdc75b7a415482b7336e0ac3fe3a75760d51ec9b0
State after apply: 89 COMPLETE / MAP07_12 CURRENT / 115 LOCKED
```

실행 범위:

- Selected microchunk ID의 transform preview.
- Validation report aggregation with local coordinates.
- Reachability heatmap from the existing probe.
- Editor-only preview window and deterministic issue ordering.
- No Authoring CSV mutation, generated CSV, starter full round-trip, or MAP07 phase exit.

`MAP07_13_MAP07_STARTER_AND_EXIT_TESTS`는 PASS 전까지 `LOCKED / DO NOT START`다.
